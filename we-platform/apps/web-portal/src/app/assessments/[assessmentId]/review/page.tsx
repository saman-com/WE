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
import { AiMockPreviewLabel } from "@/components/ai-mock-preview-label";
import { DataText } from "@/components/data-text";
import {
  approveEvidence,
  listEvidenceForAssessment,
  type Evidence,
} from "@/lib/evidence";
import { fetchAllPages } from "@/lib/paging";
import { useI18n } from "@/i18n/I18nProvider";

function canReview(profile: UserProfile): boolean {
  return profile.roles.includes("Teacher");
}

type MarkDraft = {
  mark: string;
  feedback: string;
  auditLogId?: string;
  providerName?: string;
};

export default function AssessmentReviewPage() {
  const router = useRouter();
  const { t } = useI18n();
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
          setError(t("assessments.review.teachersOnly"));
          return;
        }
        setProfile(loaded);
        const loadedAssessment = await getAssessment(stored, assessmentId);
        setAssessment(loadedAssessment);
        const [loadedSubmissions, loadedEvidence] = await Promise.all([
          listSubmissions(stored, assessmentId),
          fetchAllPages((cursor) =>
            listEvidenceForAssessment(stored, assessmentId, { cursor })
          ),
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
        setError(t("assessments.review.loadError"));
      });
  // Catalogue lookup is stable; omit `t` so toast messages do not wipe AI drafts.
  // eslint-disable-next-line react-hooks/exhaustive-deps -- reload via reloadReviewData after approve
  }, [router, assessmentId]);

  async function reloadReviewData(accessToken: string) {
    const [loadedSubmissions, loadedEvidence] = await Promise.all([
      listSubmissions(accessToken, assessmentId),
      fetchAllPages((cursor) =>
        listEvidenceForAssessment(accessToken, assessmentId, { cursor })
      ),
    ]);
    setSubmissions(loadedSubmissions);
    setEvidence(loadedEvidence);
  }

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
            providerName: draft.providerName,
          },
        },
      }));
      setMessage(t("assessments.review.draftReady"));
    } catch (err) {
      setError(err instanceof Error ? err.message : t("assessments.review.draftFailed"));
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

      setMessage(t("assessments.review.approved"));
      await reloadReviewData(token);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("assessments.review.approvalFailed"));
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
            {t("assessments.review.backToAssessments")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile || !token || !assessment) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("assessments.review.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("assessments.list.reviewSubmissions")}</h1>
          <Link href="/assessments" className="text-sm underline">
            {t("dashboard.nav.assessments")}
          </Link>
        </div>

        {error ? <p className="text-red-600">{error}</p> : null}
        {message ? <p className="text-green-700">{message}</p> : null}

        <div className="rounded-lg border border-black/10 p-6 space-y-2">
          <h2 className="font-medium">
            <DataText>{assessment.title}</DataText>
          </h2>
          {assessment.instructions ? (
            <p className="text-sm text-black/70">
              <DataText>{assessment.instructions}</DataText>
            </p>
          ) : null}
          <p className="text-sm">
            {t("assessments.review.microSkillsCount", {
              count: assessment.microSkillIds.length,
            })}
          </p>
        </div>

        {submissions.length === 0 ? (
          <p className="text-sm text-black/60">{t("assessments.review.noSubmissions")}</p>
        ) : (
          <ul className="space-y-4">
            {submissions.map((submission) => {
              const approved = evidenceFor(submission.id);
              return (
                <li key={submission.id} className="rounded-lg border border-black/10 p-6 space-y-4">
                  <div className="flex items-center justify-between gap-4">
                    <p className="font-medium">
                      {t("assessments.review.studentLabel", { id: submission.studentUserId })}
                    </p>
                    <span className="text-xs uppercase tracking-wide">
                      {approved ? t("assessments.review.approvedBadge") : submission.status}
                    </span>
                  </div>
                  <p className="text-sm text-black/70">
                    {t("assessments.review.submittedAt", {
                      date: new Date(submission.submittedAt).toLocaleString(),
                      late: submission.isLate ? t("assessments.review.lateSuffix") : "",
                    })}
                  </p>
                  <label className="block space-y-1">
                    <span className="text-sm">{t("assessments.review.responses")}</span>
                    <textarea
                      className="w-full border rounded px-3 py-2 bg-black/5"
                      rows={4}
                      value={submission.responses}
                      readOnly
                    />
                  </label>

                  {assessment.microSkillIds.length === 0 ? (
                    <p className="text-sm text-black/60">
                      {t("assessments.review.noMicroSkills")}
                    </p>
                  ) : approved ? (
                    <ul className="text-sm space-y-2">
                      {approved.microSkillMarks.map((mark) => (
                        <li key={mark.microSkillId}>
                          {mark.microSkillId}: {mark.mark} — <DataText>{mark.feedback}</DataText>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <div className="space-y-3">
                      {assessment.microSkillIds.map((microSkillId) => (
                        <div key={microSkillId} className="border rounded p-3 space-y-2">
                          <p className="text-sm font-medium">{microSkillId}</p>
                          <label className="block space-y-1">
                            <span className="text-sm">{t("assessments.review.mark")}</span>
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
                            <span className="text-sm">{t("assessments.review.feedback")}</span>
                            {(drafts[submission.id]?.[microSkillId]?.feedback ?? "").length >
                            0 ? (
                              <AiMockPreviewLabel
                                providerName={
                                  drafts[submission.id]?.[microSkillId]?.providerName
                                }
                                label={t("assessments.review.mockPreviewLabel")}
                              />
                            ) : null}
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
                            {t("assessments.review.draftWithAi")}
                          </button>
                        </div>
                      ))}
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => handleApprove(submission)}
                        className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
                      >
                        {t("assessments.review.approveEvidence")}
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
