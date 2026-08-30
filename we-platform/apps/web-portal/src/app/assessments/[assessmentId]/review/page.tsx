"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  getAssessment,
  listSubmissions,
  requestAiFeedbackDraft,
  finalizeAiFeedbackAudit,
  type Assessment,
  type AssessmentSubmission,
} from "@/lib/assessment";
import {
  approveEvidence,
  listEvidenceForAssessment,
  type Evidence,
} from "@/lib/evidence";

function canReview(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

type MarkDraft = {
  mark: string;
  feedback: string;
  auditLogId?: string;
};

export default function AssessmentReviewPage() {
  const router = useRouter();
  const params = useParams<{ assessmentId: string }>();
  const assessmentId = params.assessmentId;

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [assessment, setAssessment] = useState<Assessment | null>(null);
  const [submissions, setSubmissions] = useState<AssessmentSubmission[]>([]);
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [drafts, setDrafts] = useState<Record<string, Record<string, MarkDraft>>>(
    {}
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
        if (!canReview(loaded)) {
          setError("Only teachers can approve evidence.");
          return;
        }
        setProfile(loaded);
        const loadedAssessment = await getAssessment(stored, assessmentId);
        setAssessment(loadedAssessment);
        const [loadedSubmissions, loadedEvidence] = await Promise.all([
          listSubmissions(stored, assessmentId),
          listEvidenceForAssessment(stored, assessmentId),
        ]);
        setSubmissions(loadedSubmissions);
        setEvidence(loadedEvidence);

        const initialDrafts: Record<string, Record<string, MarkDraft>> = {};
        for (const submission of loadedSubmissions) {
          initialDrafts[submission.id] = {};
          for (const microSkillId of loadedAssessment.microSkillIds) {
            initialDrafts[submission.id][microSkillId] = {
              mark: "",
              feedback: "",
            };
          }
        }
        setDrafts(initialDrafts);
      })
      .catch(() => {
        setError("Unable to load submissions for review.");
      });
  }, [router, assessmentId, message]);

  function evidenceFor(submissionId: string): Evidence | undefined {
    return evidence.find((item) => item.submissionId === submissionId);
  }

  function updateDraft(
    submissionId: string,
    microSkillId: string,
    field: keyof MarkDraft,
    value: string
  ) {
    setDrafts((current) => ({
      ...current,
      [submissionId]: {
        ...current[submissionId],
        [microSkillId]: {
          ...current[submissionId]?.[microSkillId],
          [field]: value,
        },
      },
    }));
  }

  async function handleDraftWithAi(
    submission: AssessmentSubmission,
    microSkillId: string
  ) {
    if (!token) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      const draft = await requestAiFeedbackDraft(
        token,
        assessmentId,
        submission.id,
        microSkillId
      );
      updateDraft(submission.id, microSkillId, "feedback", draft.draftFeedback);
      setDrafts((current) => ({
        ...current,
        [submission.id]: {
          ...current[submission.id],
          [microSkillId]: {
            ...current[submission.id]?.[microSkillId],
            feedback: draft.draftFeedback,
            auditLogId: draft.auditLogId,
          },
        },
      }));
      setMessage("AI draft ready for your review. Edit before approving.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "AI draft request failed.");
    } finally {
      setBusy(false);
    }
  }

  async function handleApprove(submission: AssessmentSubmission) {
    if (!token || !assessment) {
      return;
    }

    const marks = assessment.microSkillIds.map((microSkillId) => {
      const draft = drafts[submission.id]?.[microSkillId];
      return {
        microSkillId,
        mark: Number(draft?.mark ?? 0),
        feedback: draft?.feedback?.trim() ?? "",
      };
    });

    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const evidence = await approveEvidence(token, {
        organisationId: assessment.organisationId,
        classId: assessment.classId,
        assessmentId: assessment.id,
        submissionId: submission.id,
        studentUserId: submission.studentUserId,
        title: assessment.title,
        microSkillMarks: marks,
      });

      for (const microSkillId of assessment.microSkillIds) {
        const auditLogId = drafts[submission.id]?.[microSkillId]?.auditLogId;
        const feedback = drafts[submission.id]?.[microSkillId]?.feedback ?? "";
        if (auditLogId) {
          await finalizeAiFeedbackAudit(
            token,
            auditLogId,
            feedback,
            evidence.id
          );
        }
      }

      setMessage("Submission approved. Evidence recorded on the student learning profile.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Approval failed.");
    } finally {
      setBusy(false);
    }
  }

  if (error && !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/assessments" className="underline">
            Back to assessments
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token || !assessment) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading submissions...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Review submissions</h1>
          <Link href="/assessments" className="text-sm underline">
            Assessments
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">{assessment.title}</h2>
          {assessment.instructions ? (
            <p className="text-sm text-black/70">{assessment.instructions}</p>
          ) : null}
          <p className="text-sm">
            Micro-skills: {assessment.microSkillIds.length}
          </p>
        </div>

        {submissions.length === 0 ? (
          <p className="text-sm text-black/60">No student submissions yet.</p>
        ) : (
          <ul className="space-y-4">
            {submissions.map((submission) => {
              const approved = evidenceFor(submission.id);
              return (
                <li key={submission.id} className="rounded-lg border border-black/10 p-6 space-y-4">
                  <div className="flex items-center justify-between gap-4">
                    <p className="font-medium">Student {submission.studentUserId}</p>
                    <span className="text-xs uppercase tracking-wide">
                      {approved ? "Approved" : submission.status}
                    </span>
                  </div>
                  <p className="text-sm text-black/70">
                    Submitted {new Date(submission.submittedAt).toLocaleString()}
                    {submission.isLate ? " (late)" : ""}
                  </p>
                  <label className="block space-y-1">
                    <span className="text-sm">Responses</span>
                    <textarea
                      className="w-full border rounded px-3 py-2 bg-black/5"
                      rows={4}
                      value={submission.responses}
                      readOnly
                    />
                  </label>

                  {assessment.microSkillIds.length === 0 ? (
                    <p className="text-sm text-black/60">
                      This assessment has no micro-skills linked, so marks cannot be recorded.
                    </p>
                  ) : approved ? (
                    <ul className="text-sm space-y-2">
                      {approved.microSkillMarks.map((mark) => (
                        <li key={mark.microSkillId}>
                          {mark.microSkillId}: {mark.mark} — {mark.feedback}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <div className="space-y-3">
                      {assessment.microSkillIds.map((microSkillId) => (
                        <div key={microSkillId} className="border rounded p-3 space-y-2">
                          <p className="text-sm font-medium">{microSkillId}</p>
                          <label className="block space-y-1">
                            <span className="text-sm">Mark</span>
                            <input
                              className="w-full border rounded px-3 py-2"
                              type="number"
                              value={drafts[submission.id]?.[microSkillId]?.mark ?? ""}
                              onChange={(event) =>
                                updateDraft(
                                  submission.id,
                                  microSkillId,
                                  "mark",
                                  event.target.value
                                )
                              }
                            />
                          </label>
                          <label className="block space-y-1">
                            <span className="text-sm">Feedback</span>
                            <textarea
                              className="w-full border rounded px-3 py-2"
                              rows={2}
                              value={drafts[submission.id]?.[microSkillId]?.feedback ?? ""}
                              onChange={(event) =>
                                updateDraft(
                                  submission.id,
                                  microSkillId,
                                  "feedback",
                                  event.target.value
                                )
                              }
                            />
                          </label>
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() => handleDraftWithAi(submission, microSkillId)}
                            className="rounded border border-black/20 px-3 py-1 text-sm disabled:opacity-50"
                          >
                            Draft with AI
                          </button>
                        </div>
                      ))}
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => handleApprove(submission)}
                        className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
                      >
                        Approve evidence
                      </button>
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}
