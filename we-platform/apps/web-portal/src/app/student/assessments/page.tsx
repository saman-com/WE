"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  listClasses,
  listOrganisations,
  type Organisation,
  type SchoolClass,
} from "@/lib/organisation";
import {
  getMySubmission,
  listAssessments,
  submitAssessment,
  type Assessment,
  type AssessmentSubmission,
} from "@/lib/assessment";
import { useI18n } from "@/i18n/I18nProvider";

type ClassScope = {
  organisationId: string;
  schoolClass: SchoolClass;
};

function isStudent(profile: UserProfile): boolean {
  return profile.roles.includes("Student");
}

function requestedParam(name: "classId" | "assessmentId"): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return new URLSearchParams(window.location.search).get(name);
}

export default function StudentAssessmentsPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [classScopes, setClassScopes] = useState<ClassScope[]>([]);
  const [selectedClassId, setSelectedClassId] = useState("");
  const [assessments, setAssessments] = useState<Assessment[]>([]);
  const [assessmentsHasMore, setAssessmentsHasMore] = useState(false);
  const [assessmentsCursor, setAssessmentsCursor] = useState<string | null>(null);
  const [loadingMoreAssessments, setLoadingMoreAssessments] = useState(false);
  const [selectedAssessmentId, setSelectedAssessmentId] = useState("");
  const [submission, setSubmission] = useState<AssessmentSubmission | null>(null);
  const [responses, setResponses] = useState("");

  const selectedScope = classScopes.find(
    (scope) => scope.schoolClass.id === selectedClassId
  );
  const selectedAssessment = assessments.find(
    (assessment) => assessment.id === selectedAssessmentId
  );

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
          setError(t("student.assessments.studentsOnly"));
          return;
        }
        setProfile(loaded);
        try {
          const organisations = await listOrganisations(stored);
          const scoped = await Promise.all(
            organisations.map(async (organisation: Organisation) => {
              const classes = await listClasses(stored, organisation.id);
              return classes.map((schoolClass) => ({
                organisationId: organisation.id,
                schoolClass,
              }));
            })
          );
          const flattened = scoped.flat();
          setClassScopes(flattened);
          const requested = flattened.find(
            (scope) => scope.schoolClass.id === requestedParam("classId")
          );
          const initial = requested ?? flattened[0];
          if (initial) {
            setSelectedClassId(initial.schoolClass.id);
          }
        } catch {
          setClassScopes([]);
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        router.replace("/login");
      });
  }, [router, t]);

  useEffect(() => {
    if (!token || !selectedScope) {
      setAssessments([]);
      setAssessmentsHasMore(false);
      setAssessmentsCursor(null);
      setSelectedAssessmentId("");
      return;
    }

    listAssessments(token, selectedScope.organisationId, selectedScope.schoolClass.id)
      .then((page) => {
        setAssessments(page.items);
        setAssessmentsHasMore(page.hasMore);
        setAssessmentsCursor(page.nextCursor);
        const initial =
          page.items.find((item) => item.id === requestedParam("assessmentId")) ?? page.items[0];
        setSelectedAssessmentId(initial?.id ?? "");
        setError(null);
      })
      .catch(() => {
        setAssessments([]);
        setAssessmentsHasMore(false);
        setAssessmentsCursor(null);
        setSelectedAssessmentId("");
        setError(t("common.requestFailed"));
      });
  }, [token, selectedScope, message, t]);

  async function loadMoreAssessments() {
    if (!token || !selectedScope || !assessmentsCursor || loadingMoreAssessments) {
      return;
    }
    setLoadingMoreAssessments(true);
    try {
      const page = await listAssessments(
        token,
        selectedScope.organisationId,
        selectedScope.schoolClass.id,
        { cursor: assessmentsCursor }
      );
      setAssessments((current) => [...current, ...page.items]);
      setAssessmentsHasMore(page.hasMore);
      setAssessmentsCursor(page.nextCursor);
    } finally {
      setLoadingMoreAssessments(false);
    }
  }

  useEffect(() => {
    if (!token || !selectedAssessmentId) {
      setSubmission(null);
      setResponses("");
      return;
    }

    getMySubmission(token, selectedAssessmentId)
      .then((loaded) => {
        setSubmission(loaded);
        setResponses(loaded?.responses ?? "");
      })
      .catch(() => {
        setSubmission(null);
        setResponses("");
      });
  }, [token, selectedAssessmentId, message]);

  async function handleSubmit() {
    if (!token || !selectedAssessmentId || submission) {
      return;
    }

    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const created = await submitAssessment(token, selectedAssessmentId, responses);
      setSubmission(created);
      setMessage(
        created.isLate
          ? t("student.assessments.submittedLate")
          : t("student.assessments.submittedOk")
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : t("student.assessments.submissionFailed"));
    } finally {
      setBusy(false);
    }
  }

  if (error && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/dashboard" className="underline">
            {t("common.backToDashboard")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("common.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("dashboard.nav.myAssessments")}</h1>
          <Link href="/student" className="text-sm underline">
            {t("student.myLearning")}
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("assessments.scope.classLabel")}</h2>
          {classScopes.length === 0 ? (
            <p className="text-sm text-black/60">{t("student.assessments.notEnrolled")}</p>
          ) : (
            <select
              className="w-full border rounded px-3 py-2"
              value={selectedClassId}
              onChange={(event) => setSelectedClassId(event.target.value)}
            >
              {classScopes.map((scope) => (
                <option key={scope.schoolClass.id} value={scope.schoolClass.id}>
                  {scope.schoolClass.name} ({scope.schoolClass.code})
                </option>
              ))}
            </select>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <h2 className="font-medium">{t("student.assessments.publishedTitle")}</h2>
          {assessments.length === 0 ? (
            <p className="text-sm text-black/60">{t("student.assessments.noPublished")}</p>
          ) : (
            <ul className="space-y-3">
              {assessments.map((assessment) => (
                <li key={assessment.id}>
                  <button
                    type="button"
                    className={`w-full text-left border rounded p-3 ${
                      selectedAssessmentId === assessment.id ? "border-black" : "border-black/10"
                    }`}
                    onClick={() => setSelectedAssessmentId(assessment.id)}
                  >
                    <p className="font-medium">{assessment.title}</p>
                    {assessment.dueAt ? (
                      <p className="text-sm text-black/70">
                        {t("assessments.list.duePrefix")} {new Date(assessment.dueAt).toLocaleString()}
                      </p>
                    ) : null}
                  </button>
                </li>
              ))}
            </ul>
          )}
          {assessmentsHasMore ? (
            <button
              type="button"
              disabled={loadingMoreAssessments}
              onClick={loadMoreAssessments}
              className="text-sm underline disabled:opacity-50"
            >
              {loadingMoreAssessments ? t("common.loading") : t("common.loadMore")}
            </button>
          ) : null}
        </div>

        {selectedAssessment ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <h2 className="font-medium">{selectedAssessment.title}</h2>
            {selectedAssessment.instructions ? (
              <p className="text-sm text-black/70">{selectedAssessment.instructions}</p>
            ) : null}
            {selectedAssessment.dueAt ? (
              <p className="text-sm">
                {t("assessments.list.duePrefix")} {new Date(selectedAssessment.dueAt).toLocaleString()}
              </p>
            ) : null}

            {submission ? (
              <div className="space-y-3">
                <p className="text-sm text-green-700">
                  {t("student.assessments.submittedOn", {
                    date: new Date(submission.submittedAt).toLocaleString(),
                    late: submission.isLate ? t("student.assessments.lateSuffix") : "",
                    status: submission.status,
                  })}
                </p>
                <label className="block space-y-1">
                  <span className="text-sm">{t("student.assessments.yourResponses")}</span>
                  <textarea
                    className="w-full border rounded px-3 py-2 bg-black/5"
                    rows={6}
                    value={submission.responses}
                    readOnly
                  />
                </label>
                <p className="text-sm text-black/60">
                  {t("student.assessments.cannotEdit")}
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                <label className="block space-y-1">
                  <span className="text-sm">{t("student.assessments.yourResponses")}</span>
                  <textarea
                    className="w-full border rounded px-3 py-2"
                    rows={6}
                    value={responses}
                    onChange={(event) => setResponses(event.target.value)}
                    placeholder={t("student.assessments.responsesPlaceholder")}
                  />
                </label>
                <button
                  type="button"
                  disabled={busy || responses.trim().length === 0}
                  onClick={handleSubmit}
                  className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
                >
                  {t("student.assessments.submit")}
                </button>
              </div>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}
