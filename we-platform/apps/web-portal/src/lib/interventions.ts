const interventionApiUrl =
  process.env.NEXT_PUBLIC_INTERVENTION_API_URL ?? "http://localhost:8092";

export type InterventionStatus = "Planned" | "Active" | "Completed" | "Closed";

export type Intervention = {
  id: string;
  organisationId: string;
  studentUserId: string;
  learningGapId: string;
  assignedTeacherUserId: string;
  plannedActions: string;
  notes: string;
  outcome: string | null;
  status: InterventionStatus;
  plannedStartAt: string | null;
  plannedEndAt: string | null;
  reviewAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export type StudentInterventions = {
  studentUserId: string;
  interventions: Intervention[];
};

export type CreateInterventionInput = {
  organisationId: string;
  studentUserId: string;
  learningGapId: string;
  plannedActions: string;
  notes?: string;
  plannedStartAt?: string;
  plannedEndAt?: string;
  reviewAt?: string;
};

export type GapInterventionContext = {
  learningGapId: string;
  microSkillId: string;
  severity: string;
  urgency: string;
  explanation: string;
  studentUserId: string;
};

export type CreateInterventionFromGapParams = {
  organisationId: string;
  learningGapId: string;
  microSkillId: string;
  severity: string;
  urgency: string;
  explanation: string;
  returnTo?: string;
};

const storedId = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

export type InterventionFacts = {
  skill: string;
  expected: string | null;
  demonstrated: string | null;
};

/** Pull the micro-skill name and mastery codes out of a stored diagnostic sentence. */
export function interventionFacts(summary: string): InterventionFacts {
  const expected = summary.match(/Expected mastery:\s*([A-Za-z]+)/)?.[1] ?? null;
  const demonstrated = summary.match(/Demonstrated:\s*([A-Za-z]+)/)?.[1] ?? null;
  const marked = [...summary.matchAll(/micro-skill\s+([^:\n]+?)\s+marked/gi)];
  const micro = marked.at(-1)?.[1]?.trim() ?? "";
  const skill = micro && !/^unknown skill$/i.test(micro) ? micro : summary.trim();
  return { skill, expected, demonstrated };
}

/**
 * The evidence id is not a curriculum skill, so a saved sentence can already
 * say "Unknown skill" where that id used to be. The gap's micro-skill name is
 * the same lookup the create form uses.
 */
export function withResolvedSkill(
  text: string,
  skill: string | undefined,
  unknownSkill: string
): string {
  const name = skill?.trim() ?? "";
  if (!name || name.toLowerCase() === unknownSkill.trim().toLowerCase() || !text.includes(unknownSkill)) {
    return text;
  }
  return text.replaceAll(`Evidence ${unknownSkill}`, name).replaceAll(unknownSkill, name);
}

export function withoutStoredIds(
  text: string,
  namesById: Record<string, string>
): string {
  let named = text;
  for (const [id, name] of Object.entries(namesById)) {
    if (id && name) {
      named = named.replaceAll(id, name);
    }
  }
  return named
    .replace(/Evidence\s+[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, "")
    .replace(storedId, "")
    .replace(/[ \t]{2,}/g, " ")
    .replace(/[ \t]+\n/g, "\n")
    .replace(/\n{3,}/g, "\n\n")
    .replace(/[ \t]+([.,:;])/g, "$1")
    .trim();
}

export function buildSuggestedInterventionActions(
  context: GapInterventionContext,
  skillName?: string
): string {
  const severity = context.severity.toLowerCase();
  const urgency = context.urgency.toLowerCase();
  const actions =
    urgency === "high" || severity === "high"
      ? "Schedule targeted guided practice, re-teach key concepts, and run a formative check within one week."
      : "Provide scaffolded practice and monitor progress through the next assessment cycle.";

  return [
    `Address ${severity} severity / ${urgency} urgency gap in ${skillName?.trim() || "this skill"}.`,
    context.explanation,
    `Suggested actions: ${actions}`,
  ].join("\n\n");
}

export function buildCreateInterventionFromGapUrl(
  studentUserId: string,
  params: CreateInterventionFromGapParams
): string {
  const search = new URLSearchParams({
    organisationId: params.organisationId,
    learningGapId: params.learningGapId,
    microSkillId: params.microSkillId,
    severity: params.severity,
    urgency: params.urgency,
    explanation: params.explanation,
  });
  if (params.returnTo) {
    search.set("returnTo", params.returnTo);
  }
  return `/teacher/students/${encodeURIComponent(studentUserId)}/interventions/new?${search.toString()}`;
}

export type PatchInterventionInput = {
  status?: InterventionStatus;
  notes?: string;
  outcome?: string;
  plannedActions?: string;
};

async function handleResponse<T>(response: Response, action: string): Promise<T> {
  if (!response.ok) {
    throw new Error(`${action} failed (${response.status}).`);
  }
  return response.json() as Promise<T>;
}

export function fetchStudentInterventions(
  token: string,
  studentUserId: string
): Promise<StudentInterventions> {
  return fetch(
    `${interventionApiUrl}/api/v1/interventions?studentUserId=${encodeURIComponent(studentUserId)}`,
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  ).then((response) => handleResponse<StudentInterventions>(response, "Student interventions request"));
}

export function fetchIntervention(token: string, interventionId: string): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions/${interventionId}`, {
    headers: { Authorization: `Bearer ${token}` },
  }).then((response) => handleResponse<Intervention>(response, "Intervention request"));
}

export function createIntervention(
  token: string,
  input: CreateInterventionInput
): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  }).then((response) => handleResponse<Intervention>(response, "Create intervention request"));
}

export function patchIntervention(
  token: string,
  interventionId: string,
  input: PatchInterventionInput
): Promise<Intervention> {
  return fetch(`${interventionApiUrl}/api/v1/interventions/${interventionId}`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  }).then((response) => handleResponse<Intervention>(response, "Update intervention request"));
}
