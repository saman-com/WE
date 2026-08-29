"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspaceTimelineEntry,
} from "@/lib/student-workspace";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentProgressPage() {
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [timeline, setTimeline] = useState<StudentWorkspaceTimelineEntry[]>([]);
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
        setTimeline(workspace.timeline);
      })
      .catch(() => {
        setError("Unable to load your progress timeline.");
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
        <p>Loading progress timeline...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Progress timeline</h1>
            <p className="text-sm text-black/60 mt-1">
              Your Student Learning Profile — evidence of growth over time.
            </p>
          </div>
          <Link href="/student" className="text-sm underline">
            My learning
          </Link>
        </div>

        {timeline.length === 0 ? (
          <p className="text-sm text-black/60">
            Your timeline is empty. Approved evidence from assessments will appear here as you learn.
          </p>
        ) : (
          <ol className="relative border-l border-black/20 ml-3 space-y-6">
            {timeline.map((entry) => (
              <li key={entry.id} className="ml-6">
                <span className="absolute -left-1.5 mt-1.5 h-3 w-3 rounded-full bg-black" />
                <p className="font-medium">{entry.title}</p>
                <p className="text-sm text-black/60">
                  {new Date(entry.recordedAt).toLocaleString()}
                </p>
              </li>
            ))}
          </ol>
        )}
      </div>
    </div>
  );
}
