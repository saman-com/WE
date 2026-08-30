"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { fetchStudentGaps, type StudentLearningGaps } from "@/lib/gaps";
import { createIntervention } from "@/lib/interventions";

export default function CreateInterventionPage() {
  const router = useRouter();
  const params = useParams<{ studentUserId: string }>();
  const searchParams = useSearchParams();
  const studentUserId = params.studentUserId;
  const preselectedGapId = searchParams.get("learningGapId");
  const organisationId = searchParams.get("organisationId");

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [gaps, setGaps] = useState<StudentLearningGaps | null>(null);
  const [learningGapId, setLearningGapId] = useState(preselectedGapId ?? "");
  const [plannedActions, setPlannedActions] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        const isTeacherOrAdmin =
          loaded.roles.includes("Teacher") || loaded.roles.includes("SystemAdministrator");
        if (!isTeacherOrAdmin) {
          setError("Only teachers can create interventions.");
          return;
        }

        setViewer(loaded);
        const studentGaps = await fetchStudentGaps(token, studentUserId);
        setGaps(studentGaps);

        if (preselectedGapId) {
          const gap = studentGaps.gaps.find((item) => item.id === preselectedGapId);
          if (gap) {
            setPlannedActions(
              `Address ${gap.severity.toLowerCase()} severity gap in micro-skill ${gap.microSkillId}.`
            );
          }
        }
      })
      .catch(() => {
        setError("Unable to load intervention form.");
      });
  }, [router, studentUserId, preselectedGapId]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token || !organisationId || !learningGapId || !plannedActions.trim()) {
      setError("Organisation, learning gap, and planned actions are required.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const created = await createIntervention(token, {
        organisationId,
        studentUserId,
        learningGapId,
        plannedActions: plannedActions.trim(),
        notes: notes.trim() || undefined,
      });
      router.push(`/teacher/interventions/${created.id}`);
    } catch {
      setError("Unable to create intervention.");
      setSubmitting(false);
    }
  }

  if (error && !viewer) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href={`/students/${studentUserId}/profile`} className="underline">
            Back to student profile
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer || !gaps) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>Loading intervention form...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">Create intervention</h1>
          <Link href={`/students/${studentUserId}/profile`} className="text-sm underline">
            Student profile
          </Link>
        </div>

        <form onSubmit={handleSubmit} className="rounded-lg border border-black/10 p-6 space-y-4">
          <div className="space-y-1">
            <label htmlFor="learningGapId" className="text-sm font-medium">
              Learning gap
            </label>
            <select
              id="learningGapId"
              className="w-full border border-black/20 rounded p-2 text-sm"
              value={learningGapId}
              onChange={(event) => setLearningGapId(event.target.value)}
              required
            >
              <option value="">Select a gap...</option>
              {gaps.gaps.map((gap) => (
                <option key={gap.id} value={gap.id}>
                  {gap.severity} / {gap.urgency} — {gap.microSkillId}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label htmlFor="plannedActions" className="text-sm font-medium">
              Planned actions
            </label>
            <textarea
              id="plannedActions"
              className="w-full min-h-24 border border-black/20 rounded p-3 text-sm"
              value={plannedActions}
              onChange={(event) => setPlannedActions(event.target.value)}
              required
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="notes" className="text-sm font-medium">
              Notes (optional)
            </label>
            <textarea
              id="notes"
              className="w-full min-h-20 border border-black/20 rounded p-3 text-sm"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
            />
          </div>

          {error ? <p className="text-sm text-red-600">{error}</p> : null}

          <button
            type="submit"
            className="rounded bg-black text-white px-4 py-2 text-sm disabled:opacity-50"
            disabled={submitting}
          >
            Create intervention
          </button>
        </form>
      </div>
    </div>
  );
}
