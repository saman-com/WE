"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchStudentWorkspace,
  type StudentWorkspaceFeedback,
} from "@/lib/student-workspace";
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
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/student" className="underline">
            {t("student.backToMyLearning")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("student.feedback.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("student.feedback.title")}</h1>
          <Link href="/student" className="text-sm underline">
            {t("student.myLearning")}
          </Link>
        </div>

        {feedback.length === 0 ? (
          <p className="text-sm text-black/60">
            {t("student.feedback.empty")}
          </p>
        ) : (
          <ul className="space-y-4">
            {feedback.map((item) => (
              <li key={item.evidenceId} className="rounded-lg border border-black/10 p-6 space-y-3">
                <div>
                  <p className="font-medium">{item.title}</p>
                  <p className="text-sm text-black/60">
                    {t("student.feedback.reviewed", {
                      date: new Date(item.approvedAt).toLocaleString(),
                    })}
                  </p>
                </div>
                <ul className="space-y-2">
                  {item.microSkillMarks.map((mark) => (
                    <li key={mark.microSkillId} className="text-sm border-t border-black/5 pt-2">
                      <p>
                        <span className="font-medium">{t("student.feedback.markLabel")}</span> {mark.mark}
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
