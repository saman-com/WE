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

export default function StudentProfilePage() {
  const router = useRouter();
  const params = useParams<{ studentUserId: string }>();
  const studentUserId = params.studentUserId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [diagnostics, setDiagnostics] = useState<StudentDiagnostics | null>(null);
  const [gaps, setGaps] = useState<StudentLearningGaps | null>(null);
  const [interventions, setInterventions] = useState<StudentInterventions | null>(null);
  const [mastery, setMastery] = useState<StudentMastery | null>(null);
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
          setError("You do not have access to this profile.");
          return;
        }

        setViewer(loaded);
        const studentProfile = await fetchStudentProfile(stored, studentUserId);
        setProfile(studentProfile);

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
        }

        try {
          const studentInterventions = await fetchStudentInterventions(stored, studentUserId);
          setInterventions(studentInterventions);
        } catch {
          setInterventions({ studentUserId, interventions: [] });
        }
      })
      .catch(() => {
        setError("Unable to load student learning profile.");
      });
  }, [router, studentUserId]);

  async function handleRequestProgressReport() {
    if (!token || !profile || profile.enrollments.length === 0) {
      setReportMessage("Student must be enrolled in a class to generate a progress report.");
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
      setReportMessage(
        "AI-assisted draft ready. Edit below and approve before sharing."
      );
    } catch {
      setReportMessage("Unable to generate progress report draft.");
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
      setReportMessage("Progress report approved. Ready for export or sharing.");
    } catch {
      setReportMessage("Unable to approve progress report.");
    } finally {
      setReportBusy(false);
    }
  }

  const isTeacherViewer =
    viewer?.roles.includes("SystemAdministrator") || viewer?.roles.includes("Teacher");

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/dashboard" className="underline">
            Back to dashboard
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer || !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading student profile...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Student Learning Profile</h1>
          <Link href="/dashboard" className="text-sm underline">
            Dashboard
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Student</h2>
          <p>
            <span className="font-medium">User id:</span> {profile.studentUserId}
          </p>
          {viewer.id === profile.studentUserId ? (
            <>
              <p>
                <span className="font-medium">Name:</span> {viewer.name}
              </p>
              <p>
                <span className="font-medium">Email:</span> {viewer.email}
              </p>
            </>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">Class enrollment</h2>
          {profile.enrollments.length === 0 ? (
            <p className="text-sm text-black/60">No class enrollments yet.</p>
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
          <h2 className="font-medium">Evidence timeline</h2>
          {profile.evidenceTimeline.length === 0 ? (
            <p className="text-sm text-black/60">
              No approved evidence yet. Assessment data will appear here after teacher review.
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
        </div>

        {isTeacherViewer ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-4">
            <div>
              <h2 className="font-medium">AI progress report</h2>
              <p className="text-sm text-black/60 mt-1">
                Request a draft narrative from SLP and evidence context. Edit and
                approve before sharing with parents or exporting.
              </p>
            </div>
            <button
              type="button"
              onClick={handleRequestProgressReport}
              disabled={reportBusy}
              className="text-sm underline disabled:opacity-50"
            >
              Request AI draft
            </button>
            {reportDraft ? (
              <div className="space-y-2">
                <p className="text-xs font-medium text-amber-700">
                  {reportApproved
                    ? "Approved report"
                    : "AI-assisted draft — requires your approval"}
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
                    Approve for export/share
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
            <h2 className="font-medium">Micro-skill mastery</h2>
            {mastery.records.length === 0 ? (
              <p className="text-sm text-black/60">
                No mastery records yet. Mastery is calculated from all approved evidence across assessments.
              </p>
            ) : (
              <ul className="space-y-3">
                {mastery.records.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.masteryLevel} — average {item.weightedAverage}/5
                    </p>
                    <p className="text-xs text-black/60">
                      Micro-skill: {item.microSkillId} · {item.evidenceCount} evidence source
                      {item.evidenceCount === 1 ? "" : "s"} · confidence {item.confidenceScore}
                    </p>
                    <p className="text-sm">{item.explanation}</p>
                  </li>
                ))}
              </ul>
            )}
          </div>
        ) : null}

        {gaps ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-2">
            <h2 className="font-medium">Learning gaps</h2>
            {gaps.gaps.length === 0 ? (
              <p className="text-sm text-black/60">
                No learning gaps identified yet. Gaps appear when diagnostics show a difference from expected mastery.
              </p>
            ) : (
              <ul className="space-y-3">
                {gaps.gaps.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.severity} severity — {item.urgency} urgency
                    </p>
                    <p className="text-xs text-black/60">
                      Micro-skill: {item.microSkillId}
                      {item.learningObjectiveId ? ` · LO: ${item.learningObjectiveId}` : null}
                    </p>
                    <p className="text-xs text-black/60">
                      Expected: {item.expectedMastery} · Demonstrated: {item.actualMastery} (mark {item.mark}/5)
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
              <h2 className="font-medium">Interventions</h2>
              {viewer.roles.includes("Teacher") || viewer.roles.includes("SystemAdministrator") ? (
                <Link href="/teacher/interventions" className="text-sm underline">
                  Manage interventions
                </Link>
              ) : null}
            </div>
            {interventions.interventions.length === 0 ? (
              <p className="text-sm text-black/60">
                No interventions recorded yet. Teachers create interventions when a learning gap requires planned support.
              </p>
            ) : (
              <ul className="space-y-3">
                {interventions.interventions.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.status} — {item.plannedActions}
                    </p>
                    <p className="text-xs text-black/60">
                      Gap: {item.learningGapId}
                      {item.outcome ? ` · Outcome: ${item.outcome}` : null}
                    </p>
                    {item.notes ? <p className="text-sm">{item.notes}</p> : null}
                    {viewer.roles.includes("Teacher") || viewer.roles.includes("SystemAdministrator") ? (
                      <Link href={`/teacher/interventions/${item.id}`} className="text-xs underline">
                        View detail
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
            <h2 className="font-medium">Diagnostic insights</h2>
            {diagnostics.diagnostics.length === 0 ? (
              <p className="text-sm text-black/60">
                No diagnostics yet. Insights appear after approved evidence is analysed.
              </p>
            ) : (
              <ul className="space-y-3">
                {diagnostics.diagnostics.map((item) => (
                  <li key={item.id} className="rounded border border-black/5 p-3 space-y-1">
                    <p className="text-sm font-medium">
                      {item.status} — mark {item.mark}/5
                    </p>
                    <p className="text-xs text-black/60">Micro-skill: {item.microSkillId}</p>
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
