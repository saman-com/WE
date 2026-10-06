"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import {
  fetchIntervention,
  patchIntervention,
  type Intervention,
  type InterventionStatus,
} from "@/lib/interventions";
import { useI18n } from "@/i18n/I18nProvider";

const nextStatus: Partial<Record<InterventionStatus, InterventionStatus>> = {
  Planned: "Active",
  Active: "Completed",
  Completed: "Closed",
};

export default function InterventionDetailPage() {
  const router = useRouter();
  const { t } = useI18n();
  const params = useParams<{ interventionId: string }>();
  const interventionId = params.interventionId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [intervention, setIntervention] = useState<Intervention | null>(null);
  const [notes, setNotes] = useState("");
  const [outcome, setOutcome] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackError, setFeedbackError] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    Promise.all([fetchProfile(token), fetchIntervention(token, interventionId)])
      .then(([profile, loaded]) => {
        const canManage =
          profile.roles.includes("SchoolLeader") ||
          (profile.roles.includes("Teacher") && profile.id === loaded.assignedTeacherUserId);

        if (!canManage && !profile.roles.includes("Teacher")) {
          router.replace(roleHome(profile.roles));
          return;
        }

        setViewer(profile);
        setIntervention(loaded);
        setNotes(loaded.notes);
        setOutcome(loaded.outcome ?? "");
      })
      .catch(() => {
        setError(t("teacher.interventions.detail.loadError"));
      });
  }, [router, interventionId, t]);

  async function handleSaveNotes() {
    const token = localStorage.getItem("we_access_token");
    if (!token || !intervention) {
      return;
    }

    setSaving(true);
    setFeedback(null);
    setFeedbackError(false);
    try {
      const updated = await patchIntervention(token, intervention.id, { notes });
      setIntervention(updated);
      setNotes(updated.notes ?? "");
      setFeedback(t("teacher.interventions.detail.saveNotesSuccess"));
    } catch {
      setFeedbackError(true);
      setFeedback(t("teacher.interventions.detail.saveNotesError"));
    } finally {
      setSaving(false);
    }
  }

  async function handleAdvanceStatus() {
    const token = localStorage.getItem("we_access_token");
    if (!token || !intervention) {
      return;
    }

    const status = nextStatus[intervention.status];
    if (!status) {
      return;
    }

    setSaving(true);
    setFeedback(null);
    setFeedbackError(false);
    try {
      const payload =
        status === "Completed" && outcome.trim()
          ? { status, outcome: outcome.trim() }
          : { status };
      const updated = await patchIntervention(token, intervention.id, payload);
      setIntervention(updated);
      setNotes(updated.notes ?? "");
      setOutcome(updated.outcome ?? "");
    } catch {
      setFeedbackError(true);
      setFeedback(t("teacher.interventions.detail.updateStatusError"));
    } finally {
      setSaving(false);
    }
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher/interventions" className="underline">
            {t("teacher.interventions.detail.backToInterventions")}
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer || !intervention) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("teacher.interventions.detail.loading")}</p>
      </div>
    );
  }

  const canModify =
    viewer.roles.includes("Teacher") && viewer.id === intervention.assignedTeacherUserId;

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("teacher.interventions.detail.title")}</h1>
          <Link href="/teacher/interventions" className="text-sm underline">
            {t("teacher.interventions.detail.allInterventions")}
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <p>
            <span className="font-medium">{t("teacher.interventions.detail.status")}</span> {intervention.status}
          </p>
          <p>
            <span className="font-medium">{t("teacher.interventions.studentLabel")}</span>{" "}
            <Link href={`/students/${intervention.studentUserId}/profile`} className="underline">
              {intervention.studentUserId}
            </Link>
          </p>
          <p>
            <span className="font-medium">{t("teacher.interventions.detail.learningGap")}</span> {intervention.learningGapId}
          </p>
          <p>
            <span className="font-medium">{t("teacher.interventions.detail.assignedTeacher")}</span> {intervention.assignedTeacherUserId}
          </p>
          <p>
            <span className="font-medium">{t("teacher.interventions.detail.plannedActions")}</span> {intervention.plannedActions}
          </p>
          {intervention.plannedStartAt ? (
            <p className="text-sm text-black/60">
              {t("teacher.interventions.detail.timeline", {
                start: new Date(intervention.plannedStartAt).toLocaleDateString(),
              })}
              {intervention.plannedEndAt
                ? ` – ${new Date(intervention.plannedEndAt).toLocaleDateString()}`
                : null}
            </p>
          ) : null}
          {intervention.outcome ? (
            <p>
              <span className="font-medium">{t("teacher.interventions.detail.outcome")}</span> {intervention.outcome}
            </p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 id="intervention-notes-label" className="font-medium">
            {t("teacher.interventions.detail.notes")}
          </h2>
          {canModify ? (
            <>
              <textarea
                aria-labelledby="intervention-notes-label"
                className="w-full min-h-28 border border-black/20 rounded p-3 text-sm"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
              />
              <button
                type="button"
                className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
                disabled={saving}
                onClick={handleSaveNotes}
              >
                {t("teacher.interventions.detail.saveNotes")}
              </button>
            </>
          ) : (
            <p className="text-sm">{intervention.notes || t("teacher.interventions.detail.noNotesRecorded")}</p>
          )}
          {feedback ? (
            <p
              role="status"
              className={`text-sm ${feedbackError ? "text-red-600" : "text-green-700"}`}
            >
              {feedback}
            </p>
          ) : null}
        </div>

        {canModify && nextStatus[intervention.status] ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">{t("teacher.interventions.detail.statusWorkflow")}</h2>
            {intervention.status === "Active" ? (
              <textarea
                className="w-full min-h-20 border border-black/20 rounded p-3 text-sm"
                placeholder={t("teacher.interventions.detail.outcomePlaceholder")}
                value={outcome}
                onChange={(event) => setOutcome(event.target.value)}
              />
            ) : null}
            <button
              type="button"
              className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
              disabled={saving}
              onClick={handleAdvanceStatus}
            >
              {t("teacher.interventions.detail.markAs", {
                status: nextStatus[intervention.status] ?? "",
              })}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
