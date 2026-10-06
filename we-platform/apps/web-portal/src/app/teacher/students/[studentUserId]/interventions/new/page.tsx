"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { fetchStudentGaps, type LearningGap } from "@/lib/gaps";
import {
  buildSuggestedInterventionActions,
  createIntervention,
  type GapInterventionContext,
} from "@/lib/interventions";
import { useI18n } from "@/i18n/I18nProvider";

function resolveGapContext(
  gaps: LearningGap[],
  preselectedGapId: string | null,
  searchParams: URLSearchParams,
  studentUserId: string
): GapInterventionContext | null {
  if (preselectedGapId) {
    const matchedGap = gaps.find((item) => item.id === preselectedGapId);
    if (matchedGap) {
      return {
        learningGapId: matchedGap.id,
        microSkillId: matchedGap.microSkillId,
        severity: matchedGap.severity,
        urgency: matchedGap.urgency,
        explanation: matchedGap.explanation,
        studentUserId,
      };
    }
  }

  const learningGapId = searchParams.get("learningGapId");
  const microSkillId = searchParams.get("microSkillId");
  const severity = searchParams.get("severity");
  const urgency = searchParams.get("urgency");
  const explanation = searchParams.get("explanation");
  if (!learningGapId || !microSkillId || !severity || !urgency || !explanation) {
    return null;
  }

  return {
    learningGapId,
    microSkillId,
    severity,
    urgency,
    explanation,
    studentUserId,
  };
}

export default function CreateInterventionPage() {
  const router = useRouter();
  const { t } = useI18n();
  const params = useParams<{ studentUserId: string }>();
  const searchParams = useSearchParams();
  const studentUserId = params.studentUserId;
  const preselectedGapId = searchParams.get("learningGapId");
  const organisationId = searchParams.get("organisationId");
  const returnTo = searchParams.get("returnTo");

  const [viewer, setViewer] = useState<UserProfile | null>(null);
  const [gapContext, setGapContext] = useState<GapInterventionContext | null>(null);
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
        if (!loaded.roles.includes("Teacher")) {
          router.replace(roleHome(loaded.roles));
          return;
        }

        setViewer(loaded);
        const studentGaps = await fetchStudentGaps(token, studentUserId);
        const resolvedContext = resolveGapContext(
          studentGaps.gaps,
          preselectedGapId,
          searchParams,
          studentUserId
        );
        setGapContext(resolvedContext);
        if (resolvedContext) {
          setLearningGapId(resolvedContext.learningGapId);
          setPlannedActions(buildSuggestedInterventionActions(resolvedContext));
        }
      })
      .catch(() => {
        setError(t("teacher.interventions.new.loadError"));
      });
  }, [router, studentUserId, preselectedGapId, searchParams, t]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const token = localStorage.getItem("we_access_token");
    if (!token || !organisationId || !learningGapId || !plannedActions.trim()) {
      setError(t("teacher.interventions.new.requiredError"));
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
      setError(t("teacher.interventions.new.createError"));
      setSubmitting(false);
    }
  }

  if (error && !viewer) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href={returnTo ?? `/students/${studentUserId}/profile`} className="underline">
            {t("teacher.interventions.new.back")}
          </Link>
        </div>
      </div>
    );
  }

  if (!viewer) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("teacher.interventions.new.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("teacher.interventions.new.title")}</h1>
          <Link href={returnTo ?? "/teacher/interventions"} className="text-sm underline">
            {returnTo
              ? t("teacher.interventions.new.backToClassDashboard")
              : t("teacher.workspace.nav.interventions")}
          </Link>
        </div>

        {gapContext ? (
          <div className="rounded-lg border border-black/10 p-4 space-y-2 bg-black/[0.02]">
            <h2 className="text-sm font-medium">{t("teacher.interventions.new.gapContext")}</h2>
            <p className="text-sm">
              {t("teacher.interventions.studentLabel")}{" "}
              <Link href={`/students/${studentUserId}/profile`} className="underline">
                {studentUserId}
              </Link>
            </p>
            <p className="text-xs text-black/60">
              {t("teacher.interventions.new.gapMeta", {
                microSkill: gapContext.microSkillId,
                severity: gapContext.severity,
                urgency: gapContext.urgency,
              })}
            </p>
            <p className="text-sm">{gapContext.explanation}</p>
            <p className="text-xs text-black/60">
              {t("teacher.interventions.new.reviewHint")}
            </p>
          </div>
        ) : (
          <p className="text-sm text-black/60">
            {t("teacher.interventions.new.selectGapHint")}
          </p>
        )}

        <form onSubmit={handleSubmit} className="rounded-lg border border-black/10 p-6 space-y-4">
          <input type="hidden" name="learningGapId" value={learningGapId} />

          <div className="space-y-1">
            <label htmlFor="plannedActions" className="text-sm font-medium">
              {t("teacher.interventions.new.plannedActionsLabel")}
            </label>
            <textarea
              id="plannedActions"
              className="w-full min-h-32 border border-black/20 rounded p-3 text-sm"
              value={plannedActions}
              onChange={(event) => setPlannedActions(event.target.value)}
              required
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="notes" className="text-sm font-medium">
              {t("teacher.interventions.new.notesOptional")}
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
            disabled={submitting || !learningGapId}
          >
            {t("teacher.interventions.new.title")}
          </button>
        </form>
      </div>
    </div>
  );
}
