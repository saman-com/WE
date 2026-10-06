"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { DataText } from "@/components/data-text";
import { fetchProfile, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { learningLabel, loadLearningNames, loadPeople, replaceVisibleIds } from "@/lib/display-names";
import { roleHome } from "@/lib/role-home";
import {
  fetchClassEiInsights,
  type ClassActiveLearningGap,
  type ClassEiInsights,
} from "@/lib/ei-insights";
import { buildCreateInterventionFromGapUrl } from "@/lib/interventions";
import {
  fetchClassDashboard,
  type ClassDashboard,
} from "@/lib/teacher-workspace";
import {
  finalizeAiSummaryAudit,
  requestLessonSummaryDraft,
} from "@/lib/ei-summaries";
import {
  downloadReportPdf,
  generateClassProgressReport,
  isClassProgressReport,
  type ReportResponse,
} from "@/lib/reports";
import { useI18n } from "@/i18n/I18nProvider";
import { codeLabel } from "@/lib/code-labels";

function isTeacher(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

function formatDate(value: string | null): string {
  if (!value) {
    return "—";
  }
  return new Date(value).toLocaleDateString();
}

function createInterventionHref(
  gap: ClassActiveLearningGap,
  organisationId: string,
  classId: string
): string {
  return buildCreateInterventionFromGapUrl(gap.studentUserId, {
    organisationId,
    learningGapId: gap.gapId,
    microSkillId: gap.microSkillId,
    severity: gap.severity,
    urgency: gap.urgency,
    explanation: gap.explanation,
    returnTo: `/teacher/classes/${classId}?organisationId=${organisationId}`,
  });
}

export default function TeacherClassDetailPage() {
  const router = useRouter();
  const { t } = useI18n();
  const params = useParams<{ classId: string }>();
  const searchParams = useSearchParams();
  const organisationId = searchParams.get("organisationId");
  const classId = params.classId;

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [people, setPeople] = useState<DirectoryUser[]>([]);
  const [learningNames, setLearningNames] = useState<Record<string, string>>({});
  const [dashboard, setDashboard] = useState<ClassDashboard | null>(null);
  const [insights, setInsights] = useState<ClassEiInsights | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [lessonUnitId, setLessonUnitId] = useState("");
  const [lessonDraft, setLessonDraft] = useState("");
  const [lessonAuditLogId, setLessonAuditLogId] = useState<string | null>(null);
  const [lessonApproved, setLessonApproved] = useState(false);
  const [summaryMessage, setSummaryMessage] = useState<string | null>(null);
  const [summaryBusy, setSummaryBusy] = useState(false);
  const [classReport, setClassReport] = useState<ReportResponse | null>(null);
  const [reportMessage, setReportMessage] = useState<string | null>(null);
  const [reportBusy, setReportBusy] = useState(false);

  useEffect(() => {
    const stored = localStorage.getItem("we_access_token");
    if (!stored) {
      router.replace("/login");
      return;
    }

    setToken(stored);

    if (!organisationId) {
      setError(t("teacher.class.missingOrganisation"));
      return;
    }

    fetchProfile(stored)
      .then(async (loaded) => {
        if (!isTeacher(loaded)) {
          router.replace(roleHome(loaded.roles));
          return;
        }
        setProfile(loaded);
        const [directory, names] = await Promise.all([
          loadPeople(stored),
          loadLearningNames(stored, organisationId),
        ]);
        setPeople(directory);
        setLearningNames(names);
        const loadedDashboard = await fetchClassDashboard(
          stored,
          organisationId,
          classId
        );
        setDashboard(loadedDashboard);

        try {
          const loadedInsights = await fetchClassEiInsights(
            stored,
            organisationId,
            classId
          );
          setInsights(loadedInsights);
        } catch {
          setInsights({
            organisationId,
            classId,
            masteryDistribution: [],
            activeLearningGaps: [],
            recentDiagnosticTrends: [],
            studentsNeedingAttention: [],
          });
        }
      })
      .catch(() => {
        setError(t("teacher.class.loadError"));
      });
  }, [router, organisationId, classId, t]);

  async function handleRequestLessonSummary() {
    if (!token || !organisationId || !lessonUnitId.trim()) {
      return;
    }

    setSummaryBusy(true);
    setSummaryMessage(null);
    setLessonApproved(false);
    try {
      const draft = await requestLessonSummaryDraft(
        token,
        organisationId,
        classId,
        lessonUnitId.trim()
      );
      setLessonDraft(draft.draftContent);
      setLessonAuditLogId(draft.auditLogId);
      setSummaryMessage(t("teacher.class.lesson.draftReady"));
    } catch {
      setSummaryMessage(t("teacher.class.lesson.draftError"));
    } finally {
      setSummaryBusy(false);
    }
  }

  async function handleApproveLessonSummary() {
    if (!token || !lessonAuditLogId || !lessonDraft.trim()) {
      return;
    }

    setSummaryBusy(true);
    setSummaryMessage(null);
    try {
      await finalizeAiSummaryAudit(token, lessonAuditLogId, lessonDraft.trim());
      setLessonApproved(true);
      setSummaryMessage(t("teacher.class.lesson.approved"));
    } catch {
      setSummaryMessage(t("teacher.class.lesson.approveError"));
    } finally {
      setSummaryBusy(false);
    }
  }

  async function handleGenerateClassProgressReport() {
    if (!token || !organisationId) {
      return;
    }

    setReportBusy(true);
    setReportMessage(null);
    try {
      const report = await generateClassProgressReport(token, organisationId, classId);
      setClassReport(report);
      setReportMessage(t("teacher.class.report.generated"));
    } catch {
      setReportMessage(t("teacher.class.report.generateError"));
    } finally {
      setReportBusy(false);
    }
  }

  async function handleDownloadClassReportPdf() {
    if (!token || !classReport) {
      return;
    }

    setReportBusy(true);
    setReportMessage(null);
    try {
      await downloadReportPdf(
        token,
        classReport.id,
        `class-progress-${classId}.pdf`
      );
      setReportMessage(t("teacher.class.report.pdfExported"));
    } catch {
      setReportMessage(t("teacher.class.report.pdfError"));
    } finally {
      setReportBusy(false);
    }
  }

  function named(text: string) {
    return replaceVisibleIds(
      text,
      people,
      learningNames,
      t("organisation.manage.unknownPerson"),
      t("assessments.review.unknownSkill")
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher" className="underline">
            {t("teacher.interventions.backToWorkspace")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !dashboard) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("teacher.class.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="space-y-2">
          <Link href="/teacher" className="text-sm underline">
            {t("teacher.class.backToMyClasses")}
          </Link>
          <h1 className="text-2xl font-semibold">
            <DataText>{`${dashboard.class.name} (${dashboard.class.code})`}</DataText>
          </h1>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">{t("teacher.class.roster.title")}</h2>
          {dashboard.roster.length === 0 ? (
            <p className="text-sm text-black/60">{t("common.noStudentsEnrolled")}</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left border-b border-black/10">
                  <th className="py-2">{t("teacher.class.roster.student")}</th>
                  <th className="py-2">{t("teacher.class.roster.evidence")}</th>
                  <th className="py-2">{t("teacher.class.roster.latestActivity")}</th>
                  <th className="py-2">{t("teacher.class.roster.slp")}</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.roster.map((student) => (
                  <tr key={student.studentUserId} className="border-b border-black/5">
                    <td className="py-2">{personName(people, student.studentUserId, t("organisation.manage.unknownPerson"))}</td>
                    <td className="py-2">{student.evidenceCount}</td>
                    <td className="py-2">{formatDate(student.latestActivityAt)}</td>
                    <td className="py-2">
                      <Link
                        href={`/students/${student.studentUserId}/profile`}
                        className="underline"
                      >
                        {t("teacher.class.roster.viewSlp")}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {insights ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <div>
              <h2 className="font-medium">{t("teacher.class.ei.title")}</h2>
              <p className="text-sm text-black/60 mt-1">
                {t("teacher.class.ei.subtitle")}
              </p>
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">{t("teacher.class.ei.masteryTitle")}</h3>
              {insights.masteryDistribution.length === 0 ? (
                <p className="text-sm text-black/60">
                  {t("teacher.class.ei.masteryEmpty")}
                </p>
              ) : (
                <ul className="space-y-3">
                  {insights.masteryDistribution.map((item) => (
                    <li key={item.microSkillId} className="rounded border border-black/5 p-3 space-y-1">
                      <p className="text-sm font-medium">{t("teacher.class.ei.microSkill", { id: learningLabel(learningNames, item.microSkillId, t("assessments.review.unknownSkill")) })}</p>
                      <p className="text-xs text-black/60">
                        {t("teacher.class.ei.levelSummary", {
                          mastered: item.levelCounts.Mastered ?? 0,
                          proficient: item.levelCounts.Proficient ?? 0,
                          developing: item.levelCounts.Developing ?? 0,
                          notStarted: item.levelCounts.NotStarted ?? 0,
                          total: item.totalStudents,
                        })}
                      </p>
                      <p className="text-sm">{named(item.explanation)}</p>
                      {item.linkedEvidenceIds.length > 0 ? (
                        <p className="text-xs text-black/60">
                          {t("teacher.class.ei.linkedEvidence", {
                            ids: String(item.linkedEvidenceIds.length),
                          })}
                        </p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">{t("teacher.class.ei.gapsTitle")}</h3>
              {insights.activeLearningGaps.length === 0 ? (
                <p className="text-sm text-black/60">{t("teacher.class.ei.gapsEmpty")}</p>
              ) : (
                <ul className="space-y-3">
                  {insights.activeLearningGaps.map((gap) => (
                    <li key={gap.gapId} className="rounded border border-black/5 p-3 space-y-2">
                      <p className="text-sm font-medium">
                        {t("teacher.class.ei.gapSeverityUrgency", {
                          severity: codeLabel(t, gap.severity),
                          urgency: codeLabel(t, gap.urgency),
                        })}{" "}
                        ·{" "}
                        <Link href={`/students/${gap.studentUserId}/profile`} className="underline">
                          {personName(people, gap.studentUserId, t("organisation.manage.unknownPerson"))}
                        </Link>
                      </p>
                      <p className="text-xs text-black/60">
                        {t("teacher.class.ei.microSkill", { id: learningLabel(learningNames, gap.microSkillId, t("assessments.review.unknownSkill")) })}
                      </p>
                      <p className="text-sm">{named(gap.explanation)}</p>
                      {organisationId ? (
                        <Link
                          href={createInterventionHref(gap, organisationId, classId)}
                          className="inline-block text-sm underline"
                        >
                          {t("teacher.interventions.new.title")}
                        </Link>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">{t("teacher.class.ei.trendsTitle")}</h3>
              {insights.recentDiagnosticTrends.length === 0 ? (
                <p className="text-sm text-black/60">{t("teacher.class.ei.trendsEmpty")}</p>
              ) : (
                <ul className="space-y-3">
                  {insights.recentDiagnosticTrends.map((trend) => (
                    <li
                      key={`${trend.microSkillId}-${trend.status}`}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        {t(
                          trend.occurrenceCount === 1
                            ? "teacher.class.ei.occurrenceOne"
                            : "teacher.class.ei.occurrenceOther",
                          { status: codeLabel(t, trend.status), count: trend.occurrenceCount }
                        )}
                      </p>
                      <p className="text-xs text-black/60">
                        {t("teacher.class.ei.trendMeta", {
                          id: learningLabel(learningNames, trend.microSkillId, t("assessments.review.unknownSkill")),
                          date: new Date(trend.latestAt).toLocaleDateString(),
                        })}
                      </p>
                      <p className="text-sm">{named(trend.explanation)}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">{t("teacher.class.ei.attentionTitle")}</h3>
              {insights.studentsNeedingAttention.length === 0 ? (
                <p className="text-sm text-black/60">{t("teacher.class.ei.attentionEmpty")}</p>
              ) : (
                <ul className="space-y-3">
                  {insights.studentsNeedingAttention.map((student) => (
                    <li
                      key={student.studentUserId}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        <Link href={`/students/${student.studentUserId}/profile`} className="underline">
                          {personName(people, student.studentUserId, t("organisation.manage.unknownPerson"))}
                        </Link>{" "}
                        — {student.reason}
                      </p>
                      <p className="text-sm">{named(student.explanation)}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">{t("teacher.class.report.title")}</h2>
            <p className="text-sm text-black/60 mt-1">
              {t("teacher.class.report.subtitle")}
            </p>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleGenerateClassProgressReport}
              disabled={reportBusy}
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
            >
              {t("teacher.class.report.generate")}
            </button>
            {classReport ? (
              <button
                type="button"
                onClick={handleDownloadClassReportPdf}
                disabled={reportBusy}
                className="rounded border border-black/20 px-4 py-2 text-sm disabled:opacity-50"
              >
                {t("teacher.class.report.exportPdf")}
              </button>
            ) : null}
          </div>
          {classReport && isClassProgressReport(classReport.content) ? (
            <div className="space-y-2 text-sm">
              <p>
                {t("teacher.class.report.generatedLine", {
                  className: classReport.content.className,
                  date: new Date(classReport.generatedAt).toLocaleString(),
                })}
              </p>
              <p>
                {t("teacher.class.report.counts", {
                  mastery: classReport.content.masteryDistribution.length,
                  gaps: classReport.content.activeLearningGaps.length,
                  assessments: classReport.content.assessmentSummary.length,
                })}
              </p>
            </div>
          ) : null}
          {reportMessage ? (
            <p className="text-sm text-black/70">{reportMessage}</p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">{t("teacher.class.lesson.title")}</h2>
            <p className="text-sm text-black/60 mt-1">
              {t("teacher.class.lesson.subtitle")}
            </p>
          </div>
          <label className="block text-sm space-y-1">
            <span>{t("teacher.class.lesson.unitId")}</span>
            <input
              type="text"
              value={lessonUnitId}
              onChange={(event) => setLessonUnitId(event.target.value)}
              placeholder={t("teacher.class.lesson.unitIdPlaceholder")}
              className="w-full border border-black/20 rounded px-3 py-2 text-sm"
            />
          </label>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleRequestLessonSummary}
              disabled={summaryBusy || !lessonUnitId.trim()}
              className="text-sm underline disabled:opacity-50"
            >
              {t("teacher.class.lesson.requestDraft")}
            </button>
          </div>
          {lessonDraft ? (
            <div className="space-y-2">
              <p className="text-xs font-medium text-amber-700">
                {lessonApproved
                  ? t("teacher.class.lesson.approvedLabel")
                  : t("teacher.class.lesson.draftLabel")}
              </p>
              <textarea
                value={lessonDraft}
                onChange={(event) => setLessonDraft(event.target.value)}
                rows={8}
                disabled={lessonApproved}
                className="w-full border border-black/20 rounded px-3 py-2 text-sm"
              />
              {!lessonApproved ? (
                <button
                  type="button"
                  onClick={handleApproveLessonSummary}
                  disabled={summaryBusy || !lessonDraft.trim()}
                  className="text-sm underline disabled:opacity-50"
                >
                  {t("teacher.class.lesson.approve")}
                </button>
              ) : null}
            </div>
          ) : null}
          {summaryMessage ? (
            <p className="text-sm text-black/70">{summaryMessage}</p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">{t("teacher.class.assessments.title")}</h2>
          {dashboard.recentAssessments.length === 0 ? (
            <p className="text-sm text-black/60">{t("teacher.class.assessments.empty")}</p>
          ) : (
            <ul className="space-y-3">
              {dashboard.recentAssessments.map((assessment) => (
                <li key={assessment.id} className="border border-black/10 rounded p-4">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="font-medium">
                        <DataText>{assessment.title}</DataText>
                      </p>
                      <p className="text-sm text-black/60">
                        {t("teacher.class.assessments.statusDue", {
                          status: codeLabel(t, assessment.status),
                          due: formatDate(assessment.dueAt),
                        })}
                      </p>
                      <p className="text-sm text-black/60">
                        {t("teacher.class.assessments.counts", {
                          submitted: assessment.submissionCount,
                          reviewed: assessment.reviewedCount,
                          pending: Math.max(
                            assessment.submissionCount - assessment.reviewedCount,
                            0
                          ),
                        })}
                      </p>
                    </div>
                    <Link
                      href={`/assessments/${assessment.id}/review`}
                      className="text-sm underline shrink-0"
                    >
                      {t("assessments.list.reviewSubmissions")}
                    </Link>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
