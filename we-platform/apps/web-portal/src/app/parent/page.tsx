"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchChildProgress,
  fetchLinkedChildren,
  type ParentChildLink,
  type ParentChildProgress,
} from "@/lib/parent-workspace";

function isParent(profile: UserProfile): boolean {
  return profile.roles.includes("Parent");
}

export default function ParentWorkspacePage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [children, setChildren] = useState<ParentChildLink[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null);
  const [progress, setProgress] = useState<ParentChildProgress | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!isParent(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
        const linked = await fetchLinkedChildren(token);
        setChildren(linked);
        if (linked.length > 0) {
          const firstChild = linked[0].studentUserId;
          setSelectedStudentId(firstChild);
          const childProgress = await fetchChildProgress(token, firstChild);
          setProgress(childProgress);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError("Session expired. Please sign in again.");
      });
  }, [router]);

  async function handleSelectChild(studentUserId: string) {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    setSelectedStudentId(studentUserId);
    setProgress(null);
    try {
      const childProgress = await fetchChildProgress(token, studentUserId);
      setProgress(childProgress);
    } catch {
      setError("Unable to load this child's progress.");
    }
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
        <p>Loading parent workspace...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Parent workspace</h1>
            <p className="text-sm text-black/60 mt-1">
              Welcome, {profile.name}. View approved learning progress for your linked children.
            </p>
          </div>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        {children.length === 0 ? (
          <div className="rounded-lg border border-black/10 p-6">
            <p className="text-sm text-black/60">
              No children are linked to your account yet. Please contact your school administrator.
            </p>
          </div>
        ) : (
          <>
            <div className="rounded-lg border border-black/10 p-6 space-y-3">
              <h2 className="font-medium">My children</h2>
              <div className="flex flex-wrap gap-2">
                {children.map((child) => (
                  <button
                    key={child.studentUserId}
                    type="button"
                    onClick={() => handleSelectChild(child.studentUserId)}
                    className={`rounded border px-3 py-2 text-sm ${
                      selectedStudentId === child.studentUserId
                        ? "border-black bg-black text-white"
                        : "border-black/20"
                    }`}
                  >
                    Child {child.studentUserId.slice(0, 8)}
                  </button>
                ))}
              </div>
            </div>

            {!progress ? (
              <p className="text-sm text-black/60">Loading progress...</p>
            ) : (
              <>
                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">Mastery summary</h2>
                  {progress.mastery.length === 0 ? (
                    <p className="text-sm text-black/60">No mastery records yet.</p>
                  ) : (
                    <ul className="space-y-2">
                      {progress.mastery.map((item) => (
                        <li key={item.microSkillId} className="text-sm">
                          Skill {item.microSkillId.slice(0, 8)} — {item.masteryLevel}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">Assessment results</h2>
                  {progress.assessments.length === 0 ? (
                    <p className="text-sm text-black/60">No assessments to show.</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.assessments.map((assessment) => (
                        <li key={assessment.id} className="border border-black/10 rounded p-3">
                          <p className="font-medium">{assessment.title}</p>
                          <p className="text-sm text-black/60">{assessment.className}</p>
                          <p className="text-sm">
                            {assessment.hasSubmitted ? "Submitted" : "Pending"}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">Recent teacher feedback</h2>
                  {progress.feedback.length === 0 ? (
                    <p className="text-sm text-black/60">No approved feedback yet.</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.feedback.slice(0, 3).map((item) => (
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
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">Active interventions</h2>
                  {progress.activeInterventions.length === 0 ? (
                    <p className="text-sm text-black/60">No active interventions.</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.activeInterventions.map((item) => (
                        <li key={item.id} className="border border-black/10 rounded p-3">
                          <p className="font-medium">{item.summary}</p>
                          <p className="text-sm text-black/60">Status: {item.status}</p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </>
            )}
          </>
        )}
      </div>
    </div>
  );
}
