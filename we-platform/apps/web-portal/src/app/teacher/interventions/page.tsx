"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentInterventions,
  type Intervention,
  type InterventionStatus,
} from "@/lib/interventions";
import { listClasses, listOrganisations } from "@/lib/organisation";

const statusOrder: InterventionStatus[] = ["Planned", "Active", "Completed", "Closed"];

function statusBadgeClass(status: InterventionStatus): string {
  switch (status) {
    case "Planned":
      return "bg-blue-100 text-blue-800";
    case "Active":
      return "bg-amber-100 text-amber-800";
    case "Completed":
      return "bg-green-100 text-green-800";
    case "Closed":
      return "bg-black/10 text-black/70";
  }
}

export default function TeacherInterventionsPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [interventions, setInterventions] = useState<Intervention[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<InterventionStatus | "All">("All");

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        const isTeacherOrAdmin =
          loaded.roles.includes("Teacher") || loaded.roles.includes("SystemAdministrator");
        if (!isTeacherOrAdmin) {
          router.replace("/dashboard");
          return;
        }

        setProfile(loaded);
        const organisations = await listOrganisations(token);
        const classesByOrg = await Promise.all(
          organisations.map(async (organisation) => {
            const classes = await listClasses(token, organisation.id);
            return classes.flatMap((schoolClass) =>
              (schoolClass.studentUserIds ?? []).map((studentUserId) => ({
                studentUserId,
                className: schoolClass.name,
              }))
            );
          })
        );

        const studentIds = [...new Set(classesByOrg.flat().map((entry) => entry.studentUserId))];
        const allInterventions = await Promise.all(
          studentIds.map(async (studentUserId) => {
            try {
              const response = await fetchStudentInterventions(token, studentUserId);
              return response.interventions;
            } catch {
              return [];
            }
          })
        );

        setInterventions(allInterventions.flat().sort(
          (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
        ));
      })
      .catch(() => {
        setError("Unable to load interventions.");
      });
  }, [router]);

  const filtered =
    statusFilter === "All"
      ? interventions
      : interventions.filter((item) => item.status === statusFilter);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher" className="underline">
            Back to teacher workspace
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading interventions...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Interventions</h1>
            <p className="text-sm text-black/60 mt-1">
              Track planned and active support for students in your classes.
            </p>
          </div>
          <Link href="/teacher" className="text-sm underline">
            Teacher workspace
          </Link>
        </div>

        <div className="flex flex-wrap gap-2 text-sm">
          <button
            type="button"
            className={`rounded px-3 py-1 border ${statusFilter === "All" ? "border-black" : "border-black/20"}`}
            onClick={() => setStatusFilter("All")}
          >
            All
          </button>
          {statusOrder.map((status) => (
            <button
              key={status}
              type="button"
              className={`rounded px-3 py-1 border ${statusFilter === status ? "border-black" : "border-black/20"}`}
              onClick={() => setStatusFilter(status)}
            >
              {status}
            </button>
          ))}
        </div>

        {filtered.length === 0 ? (
          <div className="rounded-lg border border-black/10 p-6">
            <p className="text-sm text-black/60">
              No interventions yet. Create one from a student learning profile when a learning gap is identified.
            </p>
          </div>
        ) : (
          <ul className="space-y-3">
            {filtered.map((item) => (
              <li key={item.id} className="rounded-lg border border-black/10 p-4 space-y-2">
                <div className="flex items-start justify-between gap-4">
                  <Link href={`/teacher/interventions/${item.id}`} className="font-medium underline">
                    {item.plannedActions}
                  </Link>
                  <span className={`text-xs rounded px-2 py-0.5 ${statusBadgeClass(item.status)}`}>
                    {item.status}
                  </span>
                </div>
                <p className="text-xs text-black/60">
                  Student:{" "}
                  <Link href={`/students/${item.studentUserId}/profile`} className="underline">
                    {item.studentUserId}
                  </Link>
                  {" · "}Gap: {item.learningGapId}
                </p>
                <p className="text-sm text-black/70 line-clamp-2">{item.notes || "No notes yet."}</p>
                <p className="text-xs text-black/50">
                  Updated {new Date(item.updatedAt).toLocaleString()}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
