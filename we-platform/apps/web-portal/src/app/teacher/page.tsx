"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { listClasses, listOrganisations, type SchoolClass } from "@/lib/organisation";

type ClassWithOrganisation = SchoolClass & {
  organisationName: string;
};

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

export default function TeacherWorkspacePage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [classes, setClasses] = useState<ClassWithOrganisation[]>([]);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isTeacher(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const organisations = await listOrganisations(token);
        const scoped = await Promise.all(
          organisations.map(async (organisation) => {
            const organisationClasses = await listClasses(token, organisation.id);
            return organisationClasses.map((schoolClass) => ({
              ...schoolClass,
              organisationName: organisation.name,
            }));
          })
        );
        setClasses(scoped.flat());
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
        <p>Loading teacher workspace...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Teacher Workspace</h1>
            <p className="text-sm text-black/60 mt-1">
              Your assigned classes and day-to-day teaching tools.
            </p>
          </div>
          <button
            onClick={handleLogout}
            className="text-sm underline"
            type="button"
          >
            Sign out
          </button>
        </div>

        <div className="flex gap-4 text-sm">
          <Link href="/assessments" className="underline">
            Assessments
          </Link>
          <Link href="/curriculum" className="underline">
            Curriculum
          </Link>
          <Link href="/teacher/interventions" className="underline">
            Interventions
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">My classes</h2>
          {classes.length === 0 ? (
            <p className="text-sm text-black/60">No classes assigned to you.</p>
          ) : (
            <ul className="space-y-3">
              {classes.map((schoolClass) => (
                <li key={schoolClass.id} className="border border-black/10 rounded p-4">
                  <Link
                    href={`/teacher/classes/${schoolClass.id}?organisationId=${schoolClass.organisationId}`}
                    className="font-medium underline"
                  >
                    {schoolClass.name} ({schoolClass.code})
                  </Link>
                  <p className="text-sm text-black/60 mt-1">
                    {schoolClass.organisationName}
                  </p>
                  <p className="text-sm text-black/60">
                    {schoolClass.studentUserIds?.length ?? 0} students enrolled
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
