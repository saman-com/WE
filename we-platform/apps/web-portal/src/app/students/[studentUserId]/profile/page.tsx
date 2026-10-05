"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { fetchStudentDiagnostics, type StudentDiagnostics } from "@/lib/diagnostics";
import { fetchStudentGaps, type StudentLearningGaps } from "@/lib/gaps";
import { fetchStudentInterventions, type StudentInterventions } from "@/lib/interventions";
import { fetchStudentMastery, type StudentMastery } from "@/lib/mastery";
import { fetchStudentProfile, type StudentProfile } from "@/lib/student-learning";
import {
  finalizeAiSummaryAudit,
  requestProgressReportDraft,
} from "@/lib/ei-summaries";
import {
  fetchStudentLongitudinal,
  type StudentLongitudinalResponse,
} from "@/lib/longitudinal-analytics";
import {
  GapHistoryTimeline,
  InterventionOutcomesTimeline,
  MasteryTrendChart,
} from "@/components/longitudinal-charts";
import { useI18n } from "@/i18n/I18nProvider";

export default function StudentProfilePage() {
  const router = useRouter();
  const { t } = useI18n();
  const params = useParams<{ studentUserId: string }>();
  const studentUserId = params.studentUserId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [evidenceHasMore, setEvidenceHasMore] = useState(false);
  const [evidenceCursor, setEvidenceCursor] = useState<string | null>(null);
  const [loadingMoreEvidence, setLoadingMoreEvidence] = useState(false);
  const [diagnostics, setDiagnostics] = useState<StudentDiagnostics | null>(null);
  const [gaps, setGaps] = useState<StudentLearningGaps | null>(null);
  const [interventions, setInterventions] = useState<StudentInterventions | null>(null);
  const [mastery, setMastery] = useState<StudentMastery | null>(null);
  const [longitudinal, setLongitudinal] = useState<StudentLongitudinalResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [reportDraft, setReportDraft] = useState("");
  const [reportAuditLogId, setReportAuditLogId] = useState<string | null>(null);
  const [reportApproved, setReportApproved] = useState(false);
  const [reportMessage, setReportMessage] = useState<string | null>(null);
  const [reportBusy, setReportBusy] = useState(false);

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }

    setToken(stored);

    fetchProfile(stored)
      .then(async (loaded) => {
        const canView =
          loaded.roles.includes("SystemAdministrator") ||
          loaded.roles.includes("Teacher") ||
          (loaded.roles.includes("Student") && loaded.id === studentUserId);

        if (!canView) {
          setError(t("student.profile.noAccess"));
          return;
        }

        setViewer(loaded);
        const studentProfile = await fetchStudentProfile(stored, studentUserId);
        setProfile(studentProfile);
        setEvidenceHasMore(studentProfile.hasMore);
        setEvidenceCursor(studentProfile.nextCursor);

        const isTeacherOrAdmin =
          loaded.roles.includes("SystemAdministrator") || loaded.roles.includes("Teacher");
        if (isTeacherOrAdmin) {
          try {
            const studentDiagnostics = await fetchStudentDiagnostics(stored, studentUserId);
            setDiagnostics(studentDiagnostics);
          } catch {
            setDiagnostics({ studentUserId, diagnostics: [] });
          }

          try {
            const studentGaps = await fetchStudentGaps(stored, studentUserId);
            setGaps(studentGaps);
          } catch {
            setGaps({ studentUserId, gaps: [] });
          }

          try {
            const studentMastery = await fetchStudentMastery(stored, studentUserId);
            setMastery(studentMastery);
          } catch {
            setMastery({ studentUserId, records: [] });
          }

          if (studentProfile.enrollments.length > 0) {
            try {
              const analytics = await fetchStudentLongitudinal(
                stored,
                studentProfile.enrollments[0].organisationId,
                studentUserId
              );
              setLongitudinal(analytics);
            } catch {
              setLongitudinal(null);
            }
          }
        }

        try {
          const studentInterventions = await fetchStudentInterventions(stored, studentUserId);
          setInterventions(studentInterventions);
        } catch {
          setInterventions({ studentUserId, interventions: [] });
        }
      })
      .catch(() => {
        setError(t("student.profile.loadError"));
      });
  }, [router, studentUserId, t]);

  async function loadMoreEvidence() {
    if (!token || !evidenceCursor || loadingMoreEvidence) {
      return;
    }
    setLoadingMoreEvidence(true);
    try {
      const next = await fetchStudentProfile(token, studentUserId, { cursor: evidenceCursor });
      setProfile((current) =>
        current
          ? {
              ...current,
              evidenceTimeline: [...current.evidenceTimeline, ...next.evidenceTimeline],
              hasMore: next.hasMore,
              nextCursor: next.nextCursor,
            }
          : next
      );
      setEvidenceHasMore(next.hasMore);
      setEvidenceCursor(next.nextCursor);
    } catch {
      setError(t("student.profile.loadError"));
    } finally {
      setLoadingMoreEvidence(false);
    }
  }

  async function handleRequestProgressReport() {
    if (!token || !profile || profile.enrollments.length === 0) {
      setReportMessage(t("student.profile.report.mustBeEnrolled"));
      return;
    }

    const enrollment = profile.enrollments[0];
    setReportBusy(true);
    setReportMessage(null);
    setReportApproved(false);
    try {
      const draft = await requestProgressReportDraft(
        token,
        studentUserId,
        enrollment.organisationId,
        enrollment.classId
      );
      setReportDraft(draft.draftContent);
      setReportAuditLogId(draft.auditLogId);
      setReportMessage(t("teacher.class.lesson.draftReady"));
    } catch {
      setReportMessage(t("student.profile.report.draftError"));
    } finally {
      setReportBusy(false);
    }
  }

  async function handleApproveProgressReport() {
    if (!token || !reportAuditLogId || !reportDraft.trim()) {
      return;
    }

    setReportBusy(true);
    setReportMessage(null);
    try {
      await finalizeAiSummaryAudit(token, reportAuditLogId, reportDraft.trim());
      setReportApproved(true);
      setReportMessage(t("student.profile.report.approved"));
    } catch {
      setReportMessage(t("student.profile.report.approveError"));
    } finally {
      setReportBusy(false);
    }
  }

  const isTeacherViewer =
    viewer?.roles.includes("SystemAdministrator") || viewer?.roles.includes("Teacher");
  const canEditProgressReport = viewer?.roles.includes("Teacher") ?? false;

  if (error) {
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

  if (!viewer || !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("student.profile.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("student.profile.title")}</h1>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.dashboard")}
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">{t("student.profile.studentTitle")}</h2>
          <p>
            <span className="font-medium">{t("dashboard.profile.userIdLabel")}</span> {profile.studentUserId}
          </p>
          {viewer.id === profile.studentUserId ? (
            <>
              <p>
                <span className="font-medium">{t("dashboard.profile.nameLabel")}</span> {viewer.name}
              </p>
              <p>
                <span className="font-medium">{t("dashboard.profile.emailLabel")}</span> {viewer.email}
              </p>
            </>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">{t("student.profile.enrollmentTitle")}</h2>
          {profile.enrollments.length === 0 ? (
            <p className="text-sm text-black/60">{t("student.profile.noEnrollments")}</p>
          ) : (
            <ul className="list-disc pl-5 space-y-1">
              {profile.enrollments.map((enrollment) => (
                <li key={enrollment.classId}>
                  {enrollment.className} ({enrollment.classCode})
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">{t("student.profile.evidenceTitle")}</h2>
          {profile.evidenceTimeline.length === 0 ? (
            <p className="text-sm text-black/60">
              {t("student.profile.noEvidence")}
            </p>
          ) : (
            <ul className="list-disc pl-5 space-y-1">
              {profile.evidenceTimeline.map((entry) => (
                <li key={entry.id}>
                  {entry.title} — {new Date(entry.recordedAt).toLocaleDateString()}
                </li>
              ))}
            </ul>
          )}
          {evidenceHasMore ? (
            <button
              type="button"
              disabled={loadingMoreEvidence}
              onClick={loadMoreEvidence}
              className="text-sm underline disabled:opacity-50"
            >
              {loadingMoreEvidence ? t("common.loading") : t("common.loadMore")}
            </button>
          ) : null}
        </div>

        {canEditProgressReport ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <div>
              <h2 className="font-medium">{t("student.profile.report.title")}</h2>
              <p className="text-sm text-black/60 mt-1">
                {t("student.profile.report.subtitle")}
              </p>
            </div>
            <button
              type="button"
              onClick={handleRequestProgressReport}
              disabled={reportBusy}
              className="text-sm underline disabled:opacity-50"
            >
              {t("teacher.class.lesson.requestDraft")}
            </button>
            {reportDraft ? (
              <div className="space-y-2">
                <p className="text-xs font-medium text-amber-700">
                  {reportApproved
                    ? t("student.profile.report.approvedLabel")
                    : t("teacher.class.lesson.draftLabel")}
                </p>
                <textarea
                  value={reportDraft}
                  onChange={(event) => setReportDraft(event.target.value)}
                  rows={10}
                  disabled={reportApproved}
                  className="w-full border border-black/20 rounded px-3 py-2 text-sm"
                />
                {!reportApproved ? (
                  <button
                    type="button"
                    onClick={handleApproveProgressReport}
                    disabled={reportBusy || !reportDraft.trim()}
                    className="text-sm underline disabled:opacity-50"
                  >
                    {t("teacher.class.lesson.approve")}
                  </button>
                ) : null}
              </div>
            ) : null}
            {reportMessage ? (
              <p className="text-sm text-black/70">{reportMessage}</p>
            ) : null}
          </div>
        ) : null}

        {mastery ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">{t("student.profile.mastery.title")}</h2>
            {mastery.records.length === 0 ? (
              <p className="text-sm text-black/60">
                {t("student.profile.mastery.empty")}
              </p>
            ) : (
              <ul className="space-y-3">
                {mastery.records.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {t("student.profile.mastery.levelAverage", {
                        level: item.masteryLevel,
                        average: item.weightedAverage,
                      })}
                    </p>
                    <p className="text-xs text-black/60">
                      {t(
                        item.evidenceCount === 1
                          ? "student.profile.mastery.metaOne"
                          : "student.profile.mastery.metaOther",
                        {
                          id: item.microSkillId,
                          count: item.evidenceCount,
                          confidence: item.confidenceScore,
                        }
                      )}
                    </p>
                    <p className="text-sm">{item.explanation}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}

        {isTeacherViewer && longitudinal ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-6">
            <div>
              <h2 className="font-medium">{t("student.profile.longitudinal.title")}</h2>
              <p className="text-sm text-black/60 mt-1">
                {t("student.profile.longitudinal.subtitle")}
              </p>
            </div>
            <MasteryTrendChart points={longitudinal.masteryTrend} />
            <GapHistoryTimeline events={longitudinal.gapHistory} />
            <InterventionOutcomesTimeline outcomes={longitudinal.interventionOutcomes} />
          </div>
        ) : null}

        {gaps ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">{t("student.profile.gaps.title")}</h2>
            {gaps.gaps.length === 0 ? (
              <p className="text-sm text-black/60">
                {t("student.profile.gaps.empty")}
              </p>
            ) : (
              <ul className="space-y-3">
                {gaps.gaps.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {t("student.profile.gaps.severityUrgency", {
                        severity: item.severity,
                        urgency: item.urgency,
                      })}
                    </p>
                    <p className="text-xs text-black/60">
                      {t("student.profile.microSkill", { id: item.microSkillId })}
                      {item.learningObjectiveId
                        ? ` · ${t("student.profile.gaps.lo", { id: item.learningObjectiveId })}`
                        : null}
                    </p>
                    <p className="text-xs text-black/60">
                      {t("student.profile.gaps.expectedDemonstrated", {
                        expected: item.expectedMastery,
                        actual: item.actualMastery,
                        mark: item.mark,
                      })}
                    </p>
                    <p className="text-sm">{item.explanation}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}

        {interventions ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <div className="flex items-center justify-between">
              <h2 className="font-medium">{t("student.profile.interventions.title")}</h2>
              {viewer.roles.includes("Teacher") || viewer.roles.includes("SystemAdministrator") ? (
                <Link href="/teacher/interventions" className="text-sm underline">
                  {t("student.profile.interventions.manage")}
                </Link>
              ) : null}
            </div>
            {interventions.interventions.length === 0 ? (
              <p className="text-sm text-black/60">
                {t("student.profile.interventions.empty")}
              </p>
            ) : (
              <ul className="space-y-3">
                {interventions.interventions.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.status} — {item.plannedActions}
                    </p>
                    <p className="text-xs text-black/60">
                      {t("teacher.interventions.gapLabel", { id: item.learningGapId })}
                      {item.outcome
                        ? ` · ${t("student.profile.interventions.outcome", { outcome: item.outcome })}`
                        : null}
                    </p>
                    {item.notes ? <p className="text-sm">{item.notes}</p> : null}
                    {viewer.roles.includes("Teacher") || viewer.roles.includes("SystemAdministrator") ? (
                      <Link href={`/teacher/interventions/${item.id}`} className="text-xs underline">
                        {t("student.profile.interventions.viewDetail")}
                      </Link>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}

        {diagnostics ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">{t("student.profile.diagnostics.title")}</h2>
            {diagnostics.diagnostics.length === 0 ? (
              <p className="text-sm text-black/60">
                {t("student.profile.diagnostics.empty")}
              </p>
            ) : (
              <ul className="space-y-3">
                {diagnostics.diagnostics.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {t("student.profile.diagnostics.statusMark", {
                        status: item.status,
                        mark: item.mark,
                      })}
                    </p>
                    <p className="text-xs text-black/60">
                      {t("student.profile.microSkill", { id: item.microSkillId })}
                    </p>
                    <p className="text-sm">{item.reason}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}
