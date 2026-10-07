"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, listParentChildren, personName, type ParentTeacher, type UserProfile } from "@/lib/auth";
import { learningLabel, loadLearningNames } from "@/lib/display-names";
import { roleHome } from "@/lib/role-home";
import { ApiError } from "@/lib/api-error";
import {
  fetchChildProgress,
  fetchLinkedChildren,
  type ParentChildLink,
  type ParentChildProgress,
} from "@/lib/parent-workspace";
import { DataText } from "@/components/data-text";
import { useI18n } from "@/i18n/I18nProvider";
import { codeLabel } from "@/lib/code-labels";

function isParent(profile: UserProfile): boolean {
  return profile.roles.includes("Parent");
}

export default function ParentWorkspacePage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [children, setChildren] = useState<ParentChildLink[]>([]);
  const [childNames, setChildNames] = useState<ParentTeacher[]>([]);
  const [learningNames, setLearningNames] = useState<Record<string, string>>({});
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null);
  const [progress, setProgress] = useState<ParentChildProgress | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    let cancelled = false;
    fetchProfile(token)
      .then(async (loaded) => {
        if (cancelled) {
          return;
        }
        if (!isParent(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }
        setProfile(loaded);
        const linked = await fetchLinkedChildren(token);
        if (cancelled) {
          return;
        }
        setChildren(linked);
        const named = await listParentChildren(token).catch(() => [] as ParentTeacher[]);
        if (!cancelled) {
          setChildNames(named);
        }
        if (linked.length > 0) {
          const firstChild = linked[0].studentUserId;
          setSelectedStudentId(firstChild);
          const childProgress = await fetchChildProgress(token, firstChild);
          if (!cancelled) {
            setProgress(childProgress);
            if (childProgress.organisationId) {
              setLearningNames(await loadLearningNames(token, childProgress.organisationId));
            }
          }
        }
      })
      .catch((err: unknown) => {
        if (cancelled) {
          return;
        }
        if (err instanceof ApiError && err.status === 401) {
          localStorage.removeItem("we_access_token");
          setError(t("common.sessionExpired"));
          return;
        }
        setError(t("common.requestFailed"));
      });

    return () => {
      cancelled = true;
    };
  }, [router, t]);

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
      if (childProgress.organisationId) {
        setLearningNames(await loadLearningNames(token, childProgress.organisationId));
      }
    } catch {
      setError(t("parent.home.childProgressError"));
    }
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("parent.home.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{t("parent.home.title")}</h1>
            <p className="text-sm text-black/60 mt-1">
              {t("parent.home.welcome", { name: profile.name })}
            </p>
          </div>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.dashboard")}
          </Link>
          <Link href="/parent/messages" className="text-sm underline">
            {t("dashboard.nav.schoolMessages")}
          </Link>
        </div>

        {children.length === 0 ? (
          <div className="rounded-lg border border-black/10 p-6">
            <p className="text-sm text-black/60">
              {t("parent.home.noChildren")}
            </p>
          </div>
        ) : (
          <>
            <div className="rounded-lg border border-black/10 p-6 space-y-3">
              <h2 className="font-medium">{t("parent.home.myChildren")}</h2>
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
                    <span>{t("parent.home.childLabel")}</span>{" "}
                    <span>
                      <DataText>
                        {personName(childNames, child.studentUserId, t("organisation.manage.unknownPerson"))}
                      </DataText>
                    </span>
                  </button>
                ))}
              </div>
            </div>

            {!progress ? (
              <p className="text-sm text-black/60">{t("parent.home.loadingProgress")}</p>
            ) : (
              <>
                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">{t("parent.home.masteryTitle")}</h2>
                  {progress.mastery.length === 0 ? (
                    <p className="text-sm text-black/60">{t("parent.home.masteryEmpty")}</p>
                  ) : (
                    <ul className="space-y-2">
                      {progress.mastery.map((item) => (
                        <li key={item.microSkillId} className="space-y-1 text-sm">
                          <p>
                            {t("parent.home.skillLead")} {codeLabel(t, item.masteryLevel)}
                          </p>
                          <p>
                            <DataText>
                              {learningLabel(
                                learningNames,
                                item.microSkillId,
                                t("assessments.review.unknownSkill")
                              )}
                            </DataText>
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">{t("parent.home.resultsTitle")}</h2>
                  {progress.assessments.length === 0 ? (
                    <p className="text-sm text-black/60">{t("parent.home.resultsEmpty")}</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.assessments.map((assessment) => (
                        <li key={assessment.id} className="border border-black/10 rounded p-3">
                          <p className="font-medium">
                            <DataText>{assessment.title}</DataText>
                          </p>
                          <p className="text-sm text-black/60">
                            <DataText>{assessment.className}</DataText>
                          </p>
                          <p className="text-sm">
                            {assessment.hasSubmitted
                              ? t("parent.home.submitted")
                              : t("parent.home.pending")}
                          </p>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">{t("student.home.recentFeedbackTitle")}</h2>
                  {progress.feedback.length === 0 ? (
                    <p className="text-sm text-black/60">{t("parent.home.feedbackEmpty")}</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.feedback.slice(0, 3).map((item) => (
                        <li key={item.evidenceId} className="border border-black/10 rounded p-3">
                          <p className="font-medium">
                            <DataText>{item.title}</DataText>
                          </p>
                          <p className="text-sm text-black/60">
                            {new Date(item.approvedAt).toLocaleDateString()}
                          </p>
                          {item.microSkillMarks[0]?.feedback ? (
                            <p className="text-sm mt-1">
                              <DataText>{item.microSkillMarks[0].feedback}</DataText>
                            </p>
                          ) : null}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <div className="rounded-lg border border-black/10 p-6 space-y-3">
                  <h2 className="font-medium">{t("parent.home.interventionsTitle")}</h2>
                  {progress.activeInterventions.length === 0 ? (
                    <p className="text-sm text-black/60">{t("parent.home.interventionsEmpty")}</p>
                  ) : (
                    <ul className="space-y-3">
                      {progress.activeInterventions.map((item) => (
                        <li key={item.id} className="border border-black/10 rounded p-3">
                          <p className="font-medium">
                            <DataText>{item.summary}</DataText>
                          </p>
                          <p className="text-sm text-black/60">
                            {t("parent.home.statusLine", { status: codeLabel(t, item.status) })}
                          </p>
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
