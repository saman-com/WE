"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { listClasses, listOrganisations, type SchoolClass } from "@/lib/organisation";

export default function DashboardPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [classes, setClasses] = useState<SchoolClass[]>([]);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        setProfile(loaded);
        try {
          const organisations = await listOrganisations(token);
          const scoped = await Promise.all(
            organisations.map((organisation) => listClasses(token, organisation.id))
          );
          setClasses(scoped.flat());
        } catch {
          setClasses([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      });
  }, [router]);

  function handleLogout() {
    localStorage.removeItem("we_access_token");
    router.push("/login");
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            Back to login
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading profile...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Dashboard</h1>
          <button
            onClick={handleLogout}
            className="text-sm underline"
            type="button"
          >
            Sign out
          </button>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <p>
            <span className="font-medium">Name:</span> {profile.name}
          </p>
          <p>
            <span className="font-medium">Email:</span> {profile.email}
          </p>
          <p>
            <span className="font-medium">User id:</span> {profile.id}
          </p>
          <p>
            <span className="font-medium">Roles:</span>{" "}
            {profile.roles.join(", ")}
          </p>
        </div>

        {profile.roles.includes("SystemAdministrator") ? (
          <>
            <Link href="/organisation" className="text-sm underline">
              Organisation setup
            </Link>
            <Link href="/admin/ai-audit" className="text-sm underline block">
              AI audit logs
            </Link>
          </>
        ) : null}

        {profile.roles.includes("SystemAdministrator") ||
        profile.roles.includes("Teacher") ? (
          <>
            <Link href="/teacher" className="text-sm underline">
              Teacher workspace
            </Link>
            <Link href="/curriculum" className="text-sm underline">
              Curriculum
            </Link>
            <Link href="/assessments" className="text-sm underline">
              Assessments
            </Link>
          </>
        ) : null}

        {profile.roles.includes("Student") ? (
          <>
            <Link href="/student" className="text-sm underline">
              Student workspace
            </Link>
            <Link href="/student/assessments" className="text-sm underline">
              My assessments
            </Link>
          </>
        ) : null}

        {profile.roles.includes("Parent") ? (
          <Link href="/parent" className="text-sm underline">
            Parent workspace
          </Link>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Classes</h2>
          {classes.length === 0 ? (
            <p className="text-sm text-black/60">No classes in your scope.</p>
          ) : (
            <ul className="space-y-3">
              {classes.map((schoolClass) => (
                <li key={schoolClass.id}>
                  <p className="font-medium">
                    {schoolClass.name} ({schoolClass.code})
                  </p>
                  {schoolClass.studentUserIds && schoolClass.studentUserIds.length > 0 ? (
                    <ul className="list-disc pl-5 mt-1 space-y-1">
                      {schoolClass.studentUserIds.map((studentUserId) => (
                        <li key={studentUserId}>
                          <Link
                            href={`/students/${studentUserId}/profile`}
                            className="text-sm underline"
                          >
                            View profile — {studentUserId}
                          </Link>
                        </li>
                      ))}
                    </ul>
                  ) : profile.roles.includes("Student") ? (
                    <Link
                      href={`/students/${profile.id}/profile`}
                      className="text-sm underline"
                    >
                      View my learning profile
                    </Link>
                  ) : (
                    <p className="text-sm text-black/60">No students enrolled.</p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>

        <Link href="/" className="text-sm underline">
          Back to home
        </Link>
      </div>
    </div>
  );
}
