"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspace,
} from "@/lib/student-workspace";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentWorkspacePage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [workspace, setWorkspace] = useState<StudentWorkspace | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isStudent(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const data = await fetchStudentWorkspace(token, loaded.id);
        setWorkspace(data);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      });
  }, [router]);

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

  if (!profile || !workspace) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading your learning progress...</p>
      </div>
    );
  }

  const pending = workspace.assessments.filter((item) => !item.hasSubmitted);
  const completed = workspace.assessments.filter((item) => item.hasSubmitted);

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">My learning</h1>
            <p className="text-sm text-black/60 mt-1">
              Welcome back, {profile.name}. Here is your progress at a glance.
            </p>
          </div>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        <div className="flex flex-wrap gap-4 text-sm">
          <Link href="/student/assessments" className="underline">
            All assessments
          </Link>
          <Link href="/student/feedback" className="underline">
            Teacher feedback
          </Link>
          <Link href="/student/progress" className="underline">
            Progress timeline
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Pending assessments</h2>
          {pending.length === 0 ? (
            <p className="text-sm text-black/60">You are up to date — no pending work.</p>
          ) : (
            <ul className="space-y-3">
              {pending.map((assessment) => (
                <li key={assessment.id} className="border border-black/10 rounded p-3">
                  <p className="font-medium">{assessment.title}</p>
                  <p className="text-sm text-black/60">{assessment.className}</p>
                  {assessment.dueAt ? (
                    <p className="text-sm">
                      Due: {new Date(assessment.dueAt).toLocaleString()}
                    </p>
                  ) : null}
                  {assessment.learningObjectiveIds.length > 0 ? (
                    <p className="text-sm text-black/70 mt-1">
                      Learning objectives: {assessment.learningObjectiveIds.join(", ")}
                    </p>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Completed assessments</h2>
          {completed.length === 0 ? (
            <p className="text-sm text-black/60">No submitted work yet.</p>
          ) : (
            <ul className="space-y-3">
              {completed.map((assessment) => (
                <li key={assessment.id} className="border border-black/10 rounded p-3">
                  <p className="font-medium">{assessment.title}</p>
                  <p className="text-sm text-black/60">{assessment.className}</p>
                  {assessment.submittedAt ? (
                    <p className="text-sm text-green-700">
                      Submitted: {new Date(assessment.submittedAt).toLocaleString()}
                    </p>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Recent teacher feedback</h2>
          {workspace.feedback.length === 0 ? (
            <p className="text-sm text-black/60">
              No feedback yet. It will appear here after your teacher reviews your work.
            </p>
          ) : (
            <ul className="space-y-3">
              {workspace.feedback.slice(0, 3).map((item) => (
                <li key={item.evidenceId} className="border border-black/10 rounded p-3">
                  <p className="font-medium">{item.title}</p>
                  <p className="text-sm text-black/60">
                    {new Date(item.approvedAt).toLocaleDateString()}
                  </p>
                  {item.microSkillMarks[0]?.feedback ? (
                    <p className="text-sm mt-1">{item.microSkillMarks[0].feedback}</p>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
          {workspace.feedback.length > 0 ? (
            <Link href="/student/feedback" className="text-sm underline">
              View all feedback
            </Link>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Learning progress</h2>
          {workspace.timeline.length === 0 ? (
            <p className="text-sm text-black/60">
              Your learning timeline will grow as evidence is approved.
            </p>
          ) : (
            <ul className="list-disc pl-5 space-y-1">
              {workspace.timeline.slice(0, 5).map((entry) => (
                <li key={entry.id} className="text-sm">
                  {entry.title} — {new Date(entry.recordedAt).toLocaleDateString()}
                </li>
              ))}
            </ul>
          )}
          {workspace.timeline.length > 0 ? (
            <Link href="/student/progress" className="text-sm underline">
              View full timeline
            </Link>
          ) : null}
        </div>
      </div>
    </div>
  );
}
