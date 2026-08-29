const evidenceApiUrl =
  process.env.NEXT_PUBLIC_EVIDENCE_API_URL ?? "http://localhost:8086";

export type MicroSkillMark = {
  microSkillId: string;
  mark: number;
  feedback: string;
};

export type Evidence = {
  id: string;
  assessmentId: string;
  submissionId: string;
  studentUserId: string;
  title: string;
  status: string;
  microSkillMarks: MicroSkillMark[];
  approvedByTeacherUserId: string;
  approvedAt: string;
};

export type ApproveEvidenceInput = {
  organisationId: string;
  classId: string;
  assessmentId: string;
  submissionId: string;
  studentUserId: string;
  title: string;
  microSkillMarks: MicroSkillMark[];
};

async function evidenceRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${evidenceApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Evidence request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function listEvidenceForAssessment(
  token: string,
  assessmentId: string
): Promise<Evidence[]> {
  const params = new URLSearchParams({ assessmentId });
  return evidenceRequest<Evidence[]>(
    token,
    `/api/v1/evidence?${params.toString()}`
  );
}

export function approveEvidence(
  token: string,
  input: ApproveEvidenceInput
): Promise<Evidence> {
  return evidenceRequest<Evidence>(token, "/api/v1/evidence", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
