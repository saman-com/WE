const eiApiUrl =
  process.env.NEXT_PUBLIC_EI_API_URL ?? "http://localhost:8091";

export type AiSummaryDraft = {
  auditLogId: string;
  draftContent: string;
  promptId: string;
  promptVersion: string;
  isAiAssistedDraft: boolean;
};

export type FinalizedAiSummary = {
  auditLogId: string;
  finalApprovedAt: string;
  isAiAssistedDraft: boolean;
};

export function requestLessonSummaryDraft(
  token: string,
  organisationId: string,
  classId: string,
  unitId: string
): Promise<AiSummaryDraft> {
  return eiRequest<AiSummaryDraft>(
    token,
    `/api/v1/ei/organisations/${organisationId}/classes/${classId}/lesson-summary-draft`,
    {
      method: "POST",
      body: JSON.stringify({ unitId }),
    }
  );
}

export function requestProgressReportDraft(
  token: string,
  studentUserId: string,
  organisationId: string,
  classId: string
): Promise<AiSummaryDraft> {
  return eiRequest<AiSummaryDraft>(
    token,
    `/api/v1/ei/students/${studentUserId}/progress-report-draft`,
    {
      method: "POST",
      body: JSON.stringify({ organisationId, classId }),
    }
  );
}

export function finalizeAiSummaryAudit(
  token: string,
  auditLogId: string,
  teacherEditedContent: string
): Promise<FinalizedAiSummary> {
  return eiRequest<FinalizedAiSummary>(
    token,
    `/api/v1/ei/ai-summary-audit/${auditLogId}/finalize`,
    {
      method: "POST",
      body: JSON.stringify({ teacherEditedContent }),
    }
  );
}

async function eiRequest<T>(
  token: string,
  path: string,
  init: RequestInit
): Promise<T> {
  const response = await fetch(`${eiApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`EI summary request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}
