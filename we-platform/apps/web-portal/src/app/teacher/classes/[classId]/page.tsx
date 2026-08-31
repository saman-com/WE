"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
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
  const params = useParams<{ classId: string }>();
  const searchParams = useSearchParams();
  const organisationId = searchParams.get("organisationId");
  const classId = params.classId;

  const [profile, setProfile] = useState<UserProfile | null>(null);
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
      setError("Missing organisation context for this class.");
      return;
    }

    fetchProfile(stored)
      .then(async (loaded) => {
        if (!isTeacher(loaded)) {
          router.replace("/dashboard");
          return;
        }
        setProfile(loaded);
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
        setError("Unable to load class dashboard.");
      });
  }, [router, organisationId, classId]);

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
      setSummaryMessage(
        "AI-assisted draft ready. Edit below and approve before sharing."
      );
    } catch {
      setSummaryMessage("Unable to generate lesson summary draft.");
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
      setSummaryMessage("Lesson summary approved. Ready for export or sharing.");
    } catch {
      setSummaryMessage("Unable to approve lesson summary.");
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
      setReportMessage("Class progress report generated on demand from EI and assessment data.");
    } catch {
      setReportMessage("Unable to generate class progress report.");
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
      setReportMessage("PDF exported.");
    } catch {
      setReportMessage("Unable to export report as PDF.");
    } finally {
      setReportBusy(false);
    }
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher" className="underline">
            Back to teacher workspace
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !dashboard) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading class dashboard...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="space-y-2">
          <Link href="/teacher" className="text-sm underline">
            Back to my classes
          </Link>
          <h1 className="text-2xl font-semibold">
            {dashboard.class.name} ({dashboard.class.code})
          </h1>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Class roster</h2>
          {dashboard.roster.length === 0 ? (
            <p className="text-sm text-black/60">No students enrolled.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left border-b border-black/10">
                  <th className="py-2">Student</th>
                  <th className="py-2">Evidence</th>
                  <th className="py-2">Latest activity</th>
                  <th className="py-2">SLP</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.roster.map((student) => (
                  <tr key={student.studentUserId} className="border-b border-black/5">
                    <td className="py-2">{student.studentUserId}</td>
                    <td className="py-2">{student.evidenceCount}</td>
                    <td className="py-2">{formatDate(student.latestActivityAt)}</td>
                    <td className="py-2">
                      <Link
                        href={`/students/${student.studentUserId}/profile`}
                        className="underline"
                      >
                        View SLP summary
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
              <h2 className="font-medium">Educational Intelligence</h2>
              <p className="text-sm text-black/60 mt-1">
                Class-level mastery, gaps, and diagnostic trends with linked evidence.
              </p>
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Mastery distribution</h3>
              {insights.masteryDistribution.length === 0 ? (
                <p className="text-sm text-black/60">
                  No mastery data yet. Insights appear after approved evidence is analysed.
                </p>
              ) : (
                <ul className="space-y-3">
                  {insights.masteryDistribution.map((item) => (
                    <li key={item.microSkillId} className="rounded border border-black/5 p-3 space-y-1">
                      <p className="text-sm font-medium">Micro-skill: {item.microSkillId}</p>
                      <p className="text-xs text-black/60">
                        Mastered {item.levelCounts.Mastered ?? 0} · Proficient{" "}
                        {item.levelCounts.Proficient ?? 0} · Developing{" "}
                        {item.levelCounts.Developing ?? 0} · Not started{" "}
                        {item.levelCounts.NotStarted ?? 0} · {item.totalStudents} students
                      </p>
                      <p className="text-sm">{item.explanation}</p>
                      {item.linkedEvidenceIds.length > 0 ? (
                        <p className="text-xs text-black/60">
                          Linked evidence: {item.linkedEvidenceIds.join(", ")}
                        </p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Active learning gaps</h3>
              {insights.activeLearningGaps.length === 0 ? (
                <p className="text-sm text-black/60">No active learning gaps identified.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.activeLearningGaps.map((gap) => (
                    <li key={gap.gapId} className="rounded border border-black/5 p-3 space-y-2">
                      <p className="text-sm font-medium">
                        {gap.severity} severity · {gap.urgency} urgency ·{" "}
                        <Link href={`/students/${gap.studentUserId}/profile`} className="underline">
                          {gap.studentUserId}
                        </Link>
                      </p>
                      <p className="text-xs text-black/60">Micro-skill: {gap.microSkillId}</p>
                      <p className="text-sm">{gap.explanation}</p>
                      <p className="text-xs text-black/60">Evidence: {gap.evidenceId}</p>
                      {organisationId ? (
                        <Link
                          href={createInterventionHref(gap, organisationId, classId)}
                          className="inline-block text-sm underline"
                        >
                          Create intervention
                        </Link>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Recent diagnostic trends</h3>
              {insights.recentDiagnosticTrends.length === 0 ? (
                <p className="text-sm text-black/60">No recent diagnostic trends.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.recentDiagnosticTrends.map((trend) => (
                    <li
                      key={`${trend.microSkillId}-${trend.status}`}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        {trend.status} · {trend.occurrenceCount} occurrence
                        {trend.occurrenceCount === 1 ? "" : "s"}
                      </p>
                      <p className="text-xs text-black/60">
                        Micro-skill: {trend.microSkillId} · Latest:{" "}
                        {new Date(trend.latestAt).toLocaleDateString()}
                      </p>
                      <p className="text-sm">{trend.explanation}</p>
                      <p className="text-xs text-black/60">Evidence: {trend.evidenceId}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-2">
              <h3 className="text-sm font-medium">Students needing attention</h3>
              {insights.studentsNeedingAttention.length === 0 ? (
                <p className="text-sm text-black/60">No students flagged for attention.</p>
              ) : (
                <ul className="space-y-3">
                  {insights.studentsNeedingAttention.map((student) => (
                    <li
                      key={student.studentUserId}
                      className="rounded border border-black/5 p-3 space-y-1"
                    >
                      <p className="text-sm font-medium">
                        <Link href={`/students/${student.studentUserId}/profile`} className="underline">
                          {student.studentUserId}
                        </Link>{" "}
                        — {student.reason}
                      </p>
                      <p className="text-sm">{student.explanation}</p>
                      {student.evidenceId ? (
                        <p className="text-xs text-black/60">Evidence: {student.evidenceId}</p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">Class progress report</h2>
            <p className="text-sm text-black/60 mt-1">
              Generate an on-demand report with mastery distribution, active learning gaps,
              and assessment summary sourced from Educational Intelligence services.
            </p>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleGenerateClassProgressReport}
              disabled={reportBusy}
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
            >
              Generate report
            </button>
            {classReport ? (
              <button
                type="button"
                onClick={handleDownloadClassReportPdf}
                disabled={reportBusy}
                className="rounded border border-black/20 px-4 py-2 text-sm disabled:opacity-50"
              >
                Export PDF
              </button>
            ) : null}
          </div>
          {classReport && isClassProgressReport(classReport.content) ? (
            <div className="space-y-2 text-sm">
              <p>
                {classReport.content.className} · Generated{" "}
                {new Date(classReport.generatedAt).toLocaleString()}
              </p>
              <p>
                Mastery areas: {classReport.content.masteryDistribution.length} · Active gaps:{" "}
                {classReport.content.activeLearningGaps.length} · Assessments:{" "}
                {classReport.content.assessmentSummary.length}
              </p>
            </div>
          ) : null}
          {reportMessage ? (
            <p className="text-sm text-black/70">{reportMessage}</p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-4">
          <div>
            <h2 className="font-medium">AI lesson summary</h2>
            <p className="text-sm text-black/60 mt-1">
              Request a draft summary for a class unit. Summaries use Educational
              Intelligence and evidence context only — edit and approve before sharing.
            </p>
          </div>
          <label className="block text-sm space-y-1">
            <span>Unit id</span>
            <input
              type="text"
              value={lessonUnitId}
              onChange={(event) => setLessonUnitId(event.target.value)}
              placeholder="Curriculum unit identifier"
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
              Request AI draft
            </button>
          </div>
          {lessonDraft ? (
            <div className="space-y-2">
              <p className="text-xs font-medium text-amber-700">
                {lessonApproved
                  ? "Approved summary"
                  : "AI-assisted draft — requires your approval"}
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
                  Approve for export/share
                </button>
              ) : null}
            </div>
          ) : null}
          {summaryMessage ? (
            <p className="text-sm text-black/70">{summaryMessage}</p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Recent assessments</h2>
          {dashboard.recentAssessments.length === 0 ? (
            <p className="text-sm text-black/60">No assessments for this class yet.</p>
          ) : (
            <ul className="space-y-3">
              {dashboard.recentAssessments.map((assessment) => (
                <li key={assessment.id} className="border border-black/10 rounded p-4">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="font-medium">{assessment.title}</p>
                      <p className="text-sm text-black/60">
                        Status: {assessment.status} · Due: {formatDate(assessment.dueAt)}
                      </p>
                      <p className="text-sm text-black/60">
                        {assessment.submissionCount} submitted ·{" "}
                        {assessment.reviewedCount} reviewed ·{" "}
                        {Math.max(assessment.submissionCount - assessment.reviewedCount, 0)}{" "}
                        pending review
                      </p>
                    </div>
                    <Link
                      href={`/assessments/${assessment.id}/review`}
                      className="text-sm underline shrink-0"
                    >
                      Review submissions
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
