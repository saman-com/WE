const assessmentApiUrl =
  process.env.NEXT_PUBLIC_ASSESSMENT_API_URL ?? "http://localhost:8085";

export type Assessment = {
  id: string;
  organisationId: string;
  classId: string;
  title: string;
  instructions: string | null;
  dueAt: string | null;
  status: string;
  publishedAt: string | null;
  learningObjectiveIds: string[];
  microSkillIds: string[];
  createdByTeacherUserId: string;
  createdAt: string;
  updatedAt: string;
};

export type CreateAssessmentInput = {
  organisationId: string;
  classId: string;
  title: string;
  instructions?: string | null;
  dueAt?: string | null;
  learningObjectiveIds: string[];
  microSkillIds: string[];
};

export type UpdateAssessmentInput = {
  title: string;
  instructions?: string | null;
  dueAt?: string | null;
  learningObjectiveIds: string[];
  microSkillIds: string[];
};

async function assessmentRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${assessmentApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    throw new Error(`Assessment request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function listAssessments(
  token: string,
  organisationId: string,
  classId: string
): Promise<Assessment[]> {
  const params = new URLSearchParams({
    organisationId,
    classId,
  });
  return assessmentRequest<Assessment[]>(
    token,
    `/api/v1/assessments?${params.toString()}`
  );
}

export function createAssessment(
  token: string,
  input: CreateAssessmentInput
): Promise<Assessment> {
  return assessmentRequest<Assessment>(token, "/api/v1/assessments", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateAssessment(
  token: string,
  assessmentId: string,
  input: UpdateAssessmentInput
): Promise<Assessment> {
  return assessmentRequest<Assessment>(
    token,
    `/api/v1/assessments/${assessmentId}`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    }
  );
}

export function publishAssessment(
  token: string,
  assessmentId: string
): Promise<Assessment> {
  return assessmentRequest<Assessment>(
    token,
    `/api/v1/assessments/${assessmentId}/publish`,
    {
      method: "POST",
    }
  );
}

export function deleteAssessment(
  token: string,
  assessmentId: string
): Promise<void> {
  return assessmentRequest<void>(
    token,
    `/api/v1/assessments/${assessmentId}`,
    {
      method: "DELETE",
    }
  );
}

export type AssessmentSubmission = {
  id: string;
  assessmentId: string;
  studentUserId: string;
  responses: string;
  status: string;
  isLate: boolean;
  submittedAt: string;
  createdAt: string;
  updatedAt: string;
};

export function getAssessment(
  token: string,
  assessmentId: string
): Promise<Assessment> {
  return assessmentRequest<Assessment>(token, `/api/v1/assessments/${assessmentId}`);
}

export function getMySubmission(
  token: string,
  assessmentId: string
): Promise<AssessmentSubmission | null> {
  return fetch(`${assessmentApiUrl}/api/v1/assessments/${assessmentId}/submissions/me`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (response.status === 404) {
      return null;
    }
    if (!response.ok) {
      throw new Error(`Submission request failed (${response.status}).`);
    }
    return response.json() as Promise<AssessmentSubmission>;
  });
}

export function submitAssessment(
  token: string,
  assessmentId: string,
  responses: string
): Promise<AssessmentSubmission> {
  return assessmentRequest<AssessmentSubmission>(
    token,
    `/api/v1/assessments/${assessmentId}/submissions`,
    {
      method: "POST",
      body: JSON.stringify({ responses }),
    }
  );
}

export function listSubmissions(
  token: string,
  assessmentId: string
): Promise<AssessmentSubmission[]> {
  return assessmentRequest<AssessmentSubmission[]>(
    token,
    `/api/v1/assessments/${assessmentId}/submissions`
  );
}

export type AiFeedbackDraft = {
  auditLogId: string;
  draftFeedback: string;
  promptId: string;
  promptVersion: string;
};

export function requestAiFeedbackDraft(
  token: string,
  assessmentId: string,
  submissionId: string,
  microSkillId: string
): Promise<AiFeedbackDraft> {
  return assessmentRequest<AiFeedbackDraft>(
    token,
    `/api/v1/assessments/${assessmentId}/submissions/${submissionId}/ai-feedback-draft`,
    {
      method: "POST",
      body: JSON.stringify({ microSkillId }),
    }
  );
}

export function finalizeAiFeedbackAudit(
  token: string,
  auditLogId: string,
  teacherEditedFeedback: string,
  evidenceId: string
): Promise<{ auditLogId: string; finalApprovedAt: string }> {
  return assessmentRequest<{ auditLogId: string; finalApprovedAt: string }>(
    token,
    `/api/v1/assessments/ai-feedback-audit/${auditLogId}/finalize`,
    {
      method: "POST",
      body: JSON.stringify({ teacherEditedFeedback, evidenceId }),
    }
  );
}
