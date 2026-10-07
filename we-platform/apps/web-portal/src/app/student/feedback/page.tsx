"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { listStudentFeedback, type StudentFeedback } from "@/lib/evidence";
import { DataText } from "@/components/data-text";
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
  const [token, setToken] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<StudentFeedback[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [cursor, setCursor] = useState<string | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }

    setToken(stored);
    fetchProfile(stored)
      .then(async (loaded) => {
        if (!isStudent(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }
        setProfile(loaded);
        const page = await listStudentFeedback(stored);
        setFeedback(page.items);
        setHasMore(page.hasMore);
        setCursor(page.nextCursor);
      })
      .catch(() => {
        setError(t("student.feedback.loadError"));
      });
  }, [router, t]);

  async function loadMore() {
    if (!token || !cursor || loadingMore) {
      return;
    }
    setLoadingMore(true);
    try {
      const page = await listStudentFeedback(token, { cursor });
      setFeedback((current) => [...current, ...page.items]);
      setHasMore(page.hasMore);
      setCursor(page.nextCursor);
    } catch {
      setError(t("student.feedback.loadError"));
    } finally {
      setLoadingMore(false);
    }
  }

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
              <li key={item.id} className="space-y-3">
                <div>
                  <p className="font-semibold">
                    <DataText>{item.title}</DataText>
                  </p>
                  <p className="text-sm text-black/60">
                    {t("student.feedback.reviewed", {
                      date: new Date(item.approvedAt).toLocaleString(),
                    })}
                  </p>
                </div>
                <ul className="divide-y divide-black/10 border-y border-black/10">
                  {item.microSkillMarks.map((mark) => (
                    <li key={mark.microSkillId} className="space-y-1 py-3">
                      <p className="text-sm">
                        <DataText>{mark.feedback || t("student.feedback.markLabel")}</DataText>
                      </p>
                      <p className="text-sm text-black/60">
                        {t(`student.focus.level.${levelFromMark(mark.mark)}`)}
                      </p>
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ul>
        )}
        {hasMore ? (
          <button
            type="button"
            disabled={loadingMore}
            onClick={loadMore}
            className="text-sm underline disabled:opacity-50"
          >
            {loadingMore ? t("common.loading") : t("common.loadMore")}
          </button>
        ) : null}
      </div>
    </LearningFrame>
  );
}
