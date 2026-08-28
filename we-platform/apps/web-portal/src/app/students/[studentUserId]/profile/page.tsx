"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { fetchStudentProfile, type StudentProfile } from "@/lib/student-learning";

export default function StudentProfilePage() {
  const router = useRouter();
  const params = useParams<{ studentUserId: string }>();
  const studentUserId = params.studentUserId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        const canView =
          loaded.roles.includes("SystemAdministrator") ||
          loaded.roles.includes("Teacher") ||
          (loaded.roles.includes("Student") && loaded.id === studentUserId);

        if (!canView) {
          setError("You do not have access to this profile.");
          return;
        }

        setViewer(loaded);
        const studentProfile = await fetchStudentProfile(token, studentUserId);
        setProfile(studentProfile);
      })
      .catch(() => {
        setError("Unable to load student learning profile.");
      });
  }, [router, studentUserId]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/dashboard" className="underline">
            Back to dashboard
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer || !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading student profile...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Student Learning Profile</h1>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Student</h2>
          <p>
            <span className="font-medium">User id:</span> {profile.studentUserId}
          </p>
          {viewer.id === profile.studentUserId ? (
            <>
              <p>
                <span className="font-medium">Name:</span> {viewer.name}
              </p>
              <p>
                <span className="font-medium">Email:</span> {viewer.email}
              </p>
            </>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Class enrollment</h2>
          {profile.enrollments.length === 0 ? (
            <p className="text-sm text-black/60">No class enrollments yet.</p>
          ) : (
            <ul className="list-disc pl-5 space-y-1">
              {profile.enrollments.map((enrollment) => (
                <li key={enrollment.classId}>
                  {enrollment.className} ({enrollment.classCode})
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Evidence timeline</h2>
          {profile.evidenceTimeline.length === 0 ? (
            <p className="text-sm text-black/60">
              No approved evidence yet. Assessment data will appear here after teacher review.
            </p>
          ) : (
            <ul className="list-disc pl-5 space-y-1">
              {profile.evidenceTimeline.map((entry) => (
                <li key={entry.id}>
                  {entry.title} — {new Date(entry.recordedAt).toLocaleDateString()}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
