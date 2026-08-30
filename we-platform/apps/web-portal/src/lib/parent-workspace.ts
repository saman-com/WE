const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type ParentChildLink = {
  parentUserId: string;
  studentUserId: string;
};

export type ParentMasterySummary = {
  microSkillId: string;
  masteryLevel: string;
};

export type ParentAssessmentSummary = {
  id: string;
  className: string;
  title: string;
  dueAt: string | null;
  hasSubmitted: boolean;
  submittedAt: string | null;
};

export type ParentFeedbackMark = {
  microSkillId: string;
  mark: number;
  feedback: string;
};

export type ParentFeedbackSummary = {
  evidenceId: string;
  assessmentId: string;
  title: string;
  approvedAt: string;
  microSkillMarks: ParentFeedbackMark[];
};

export type ParentInterventionSummary = {
  id: string;
  summary: string;
  status: string;
  plannedStartAt: string | null;
  plannedEndAt: string | null;
};

export type ParentChildProgress = {
  studentUserId: string;
  mastery: ParentMasterySummary[];
  feedback: ParentFeedbackSummary[];
  assessments: ParentAssessmentSummary[];
  activeInterventions: ParentInterventionSummary[];
};

async function parentRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${organisationApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Parent workspace request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchLinkedChildren(token: string): Promise<ParentChildLink[]> {
  return parentRequest<ParentChildLink[]>(token, "/api/v1/parents/me/children");
}

export function fetchChildProgress(
  token: string,
  studentUserId: string
): Promise<ParentChildProgress> {
  return parentRequest<ParentChildProgress>(
    token,
    `/api/v1/parents/me/children/${studentUserId}/progress`
  );
}

export function linkParentToStudent(
  token: string,
  parentUserId: string,
  studentUserId: string
): Promise<ParentChildLink> {
  return parentRequest<ParentChildLink>(
    token,
    `/api/v1/parents/${parentUserId}/children`,
    {
      method: "POST",
      body: JSON.stringify({ studentUserId }),
    }
  );
}
