"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspaceTimelineEntry,
} from "@/lib/student-workspace";
import { LearningFrame } from "@/components/learning-frame";
import { useI18n } from "@/i18n/I18nProvider";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentProgressPage() {
  const router = useRouter();
  const { t } = useI18n();
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
        setError(t("student.progress.loadError"));
      });
  }, [router, t]);

  if (error) {
    return (
      <div className="we-learning flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-700">{error}</p>
          <Link href="/student" className="underline">
            {t("student.backToMyLearning")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="we-learning flex items-center justify-center p-6">
        <p>{t("student.progress.loading")}</p>
      </div>
    );
  }

  return (
    <LearningFrame eyebrow="WE" title={t("student.progress.title")}>
      <div className="space-y-6">
        <Link href="/student" className="text-sm text-black/60 underline-offset-2 hover:underline">
          {t("student.myLearning")}
        </Link>
        <p className="max-w-xl text-sm text-black/60">{t("student.progress.subtitle")}</p>
        {timeline.length === 0 ? (
          <p className="text-sm text-black/60">{t("student.progress.empty")}</p>
        ) : (
          <ol className="space-y-4 border-s border-black/15 ps-4">
            {timeline.map((entry) => (
              <li key={entry.id}>
                <p className="font-semibold">{entry.title}</p>
                <p className="text-sm text-black/60">
                  {new Date(entry.recordedAt).toLocaleString()}
                </p>
              </li>
            ))}
          </ol>
        )}
      </div>
    </LearningFrame>
  );
}
