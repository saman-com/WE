"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspaceFeedback,
} from "@/lib/student-workspace";
import { LearningFrame } from "@/components/learning-frame";
import { levelFromMark } from "@/lib/student-focus";
import { useI18n } from "@/i18n/I18nProvider";

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

export default function StudentFeedbackPage() {
  const router = useRouter();
  const { t } = useI18n();
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
        setError(t("student.feedback.loadError"));
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
        <p>{t("student.feedback.loading")}</p>
      </div>
    );
  }

  return (
    <LearningFrame eyebrow="WE" title={t("student.feedback.title")}>
      <div className="space-y-6">
        <Link href="/student" className="text-sm text-black/60 underline-offset-2 hover:underline">
          {t("student.myLearning")}
        </Link>
        {feedback.length === 0 ? (
          <p className="text-sm text-black/60">{t("student.feedback.empty")}</p>
        ) : (
          <ul className="space-y-6">
            {feedback.map((item) => (
              <li key={item.evidenceId} className="space-y-3">
                <div>
                  <p className="font-semibold">{item.title}</p>
                  <p className="text-sm text-black/60">
                    {t("student.feedback.reviewed", {
                      date: new Date(item.approvedAt).toLocaleString(),
                    })}
                  </p>
                </div>
                <ul className="divide-y divide-black/10 border-y border-black/10">
                  {item.microSkillMarks.map((mark) => (
                    <li key={mark.microSkillId} className="flex items-baseline justify-between gap-3 py-3">
                      <span className="text-sm">{mark.feedback || t("student.feedback.markLabel")}</span>
                      <span className="shrink-0 text-sm text-black/60">
                        {t(`student.focus.level.${levelFromMark(mark.mark)}`)}
                      </span>
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ul>
        )}
      </div>
    </LearningFrame>
  );
}
