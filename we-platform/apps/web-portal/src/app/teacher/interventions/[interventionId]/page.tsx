"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchIntervention,
  patchIntervention,
  type Intervention,
  type InterventionStatus,
} from "@/lib/interventions";

const nextStatus: Partial<Record<InterventionStatus, InterventionStatus>> = {
  Planned: "Active",
  Active: "Completed",
  Completed: "Closed",
};

export default function InterventionDetailPage() {
  const router = useRouter();
  const params = useParams<{ interventionId: string }>();
  const interventionId = params.interventionId;

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [intervention, setIntervention] = useState<Intervention | null>(null);
  const [notes, setNotes] = useState("");
  const [outcome, setOutcome] = useState("");
  const [error, setError] = useState<string | null>(null);
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
          profile.roles.includes("SystemAdministrator") ||
          profile.roles.includes("SchoolLeader") ||
          (profile.roles.includes("Teacher") && profile.id === loaded.assignedTeacherUserId);

        if (!canManage && !profile.roles.includes("Teacher")) {
          setError("You do not have access to this intervention.");
          return;
        }

        setViewer(profile);
        setIntervention(loaded);
        setNotes(loaded.notes);
        setOutcome(loaded.outcome ?? "");
      })
      .catch(() => {
        setError("Unable to load intervention.");
      });
  }, [router, interventionId]);

  async function handleSaveNotes() {
    const token = localStorage.getItem("we_access_token");
    if (!token || !intervention) {
      return;
    }

    setSaving(true);
    try {
      const updated = await patchIntervention(token, intervention.id, { notes });
      setIntervention(updated);
    } catch {
      setError("Unable to save notes.");
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
    try {
      const payload =
        status === "Completed" && outcome.trim()
          ? { status, outcome: outcome.trim() }
          : { status };
      const updated = await patchIntervention(token, intervention.id, payload);
      setIntervention(updated);
    } catch {
      setError("Unable to update status.");
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
            Back to interventions
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer || !intervention) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading intervention...</p>
      </div>
    );
  }

  const canModify =
    viewer.roles.includes("SystemAdministrator") ||
    viewer.roles.includes("SchoolLeader") ||
    viewer.id === intervention.assignedTeacherUserId;

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Intervention detail</h1>
          <Link href="/teacher/interventions" className="text-sm underline">
            All interventions
          </Link>
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <p>
            <span className="font-medium">Status:</span> {intervention.status}
          </p>
          <p>
            <span className="font-medium">Student:</span>{" "}
            <Link href={`/students/${intervention.studentUserId}/profile`} className="underline">
              {intervention.studentUserId}
            </Link>
          </p>
          <p>
            <span className="font-medium">Learning gap:</span> {intervention.learningGapId}
          </p>
          <p>
            <span className="font-medium">Assigned teacher:</span> {intervention.assignedTeacherUserId}
          </p>
          <p>
            <span className="font-medium">Planned actions:</span> {intervention.plannedActions}
          </p>
          {intervention.plannedStartAt ? (
            <p className="text-sm text-black/60">
              Timeline: {new Date(intervention.plannedStartAt).toLocaleDateString()}
              {intervention.plannedEndAt
                ? ` – ${new Date(intervention.plannedEndAt).toLocaleDateString()}`
                : null}
            </p>
          ) : null}
          {intervention.outcome ? (
            <p>
              <span className="font-medium">Outcome:</span> {intervention.outcome}
            </p>
          ) : null}
        </div>

        <div className="rounded-lg border border-black/10 p-6 space-y-3">
          <h2 className="font-medium">Notes</h2>
          {canModify ? (
            <>
              <textarea
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
                Save notes
              </button>
            </>
          ) : (
            <p className="text-sm">{intervention.notes || "No notes recorded."}</p>
          )}
        </div>

        {canModify && nextStatus[intervention.status] ? (
          <div className="rounded-lg border border-black/10 p-6 space-y-3">
            <h2 className="font-medium">Status workflow</h2>
            {intervention.status === "Active" ? (
              <textarea
                className="w-full min-h-20 border border-black/20 rounded p-3 text-sm"
                placeholder="Outcome summary when marking complete..."
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
              Mark as {nextStatus[intervention.status]}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
