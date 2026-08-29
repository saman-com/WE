"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspaceFeedback,
} from "@/lib/student-workspace";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentFeedbackPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [feedback, setFeedback] = useState<StudentWorkspaceFeedback[]>([]);
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
        const workspace = await fetchStudentWorkspace(token, loaded.id);
        setFeedback(workspace.feedback);
      })
      .catch(() => {
        setError("Unable to load teacher feedback.");
      });
  }, [router]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/student" className="underline">
            Back to my learning
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading feedback...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Teacher feedback</h1>
          <Link href="/student" className="text-sm underline">
            My learning
          </Link>
        </div>

        {feedback.length === 0 ? (
          <p className="text-sm text-black/60">
            No approved feedback yet. Submit assessments and your teacher will share feedback here.
          </p>
        ) : (
          <ul className="space-y-4">
            {feedback.map((item) => (
              <li key={item.evidenceId} className="rounded-lg border border-black/10 p-6 space-y-3">
                <div>
                  <p className="font-medium">{item.title}</p>
                  <p className="text-sm text-black/60">
                    Reviewed {new Date(item.approvedAt).toLocaleString()}
                  </p>
                </div>
                <ul className="space-y-2">
                  {item.microSkillMarks.map((mark) => (
                    <li key={mark.microSkillId} className="text-sm border-t border-black/5 pt-2">
                      <p>
                        <span className="font-medium">Mark:</span> {mark.mark}
                      </p>
                      {mark.feedback ? (
                        <p className="text-black/80 mt-1">{mark.feedback}</p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
