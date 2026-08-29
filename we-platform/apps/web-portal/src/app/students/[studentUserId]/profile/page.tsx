"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { fetchStudentDiagnostics, type StudentDiagnostics } from "@/lib/diagnostics";
import { fetchStudentGaps, type StudentLearningGaps } from "@/lib/gaps";
import { fetchStudentProfile, type StudentProfile } from "@/lib/student-learning";

export default function StudentProfilePage() {
  const router = useRouter();
  const params = useParams<{ studentUserId: string }>();
  const studentUserId = params.studentUserId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [diagnostics, setDiagnostics] = useState<StudentDiagnostics | null>(null);
  const [gaps, setGaps] = useState<StudentLearningGaps | null>(null);
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

        const isTeacherOrAdmin =
          loaded.roles.includes("SystemAdministrator") || loaded.roles.includes("Teacher");
        if (isTeacherOrAdmin) {
          try {
            const studentDiagnostics = await fetchStudentDiagnostics(token, studentUserId);
            setDiagnostics(studentDiagnostics);
          } catch {
            setDiagnostics({ studentUserId, diagnostics: [] });
          }

          try {
            const studentGaps = await fetchStudentGaps(token, studentUserId);
            setGaps(studentGaps);
          } catch {
            setGaps({ studentUserId, gaps: [] });
          }
        }
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

        {gaps ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">Learning gaps</h2>
            {gaps.gaps.length === 0 ? (
              <p className="text-sm text-black/60">
                No learning gaps identified yet. Gaps appear when diagnostics show a difference from expected mastery.
              </p>
            ) : (
              <ul className="space-y-3">
                {gaps.gaps.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.severity} severity — {item.urgency} urgency
                    </p>
                    <p className="text-xs text-black/60">
                      Micro-skill: {item.microSkillId}
                      {item.learningObjectiveId ? ` · LO: ${item.learningObjectiveId}` : null}
                    </p>
                    <p className="text-xs text-black/60">
                      Expected: {item.expectedMastery} · Demonstrated: {item.actualMastery} (mark {item.mark}/5)
                    </p>
                    <p className="text-sm">{item.explanation}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}

        {diagnostics ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">Diagnostic insights</h2>
            {diagnostics.diagnostics.length === 0 ? (
              <p className="text-sm text-black/60">
                No diagnostics yet. Insights appear after approved evidence is analysed.
              </p>
            ) : (
              <ul className="space-y-3">
                {diagnostics.diagnostics.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.status} — mark {item.mark}/5
                    </p>
                    <p className="text-xs text-black/60">Micro-skill: {item.microSkillId}</p>
                    <p className="text-sm">{item.reason}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}
