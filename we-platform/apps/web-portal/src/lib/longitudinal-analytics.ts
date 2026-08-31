const reportingApiUrl =
  process.env.NEXT_PUBLIC_REPORTING_API_URL ?? "http://localhost:8096";

export type MasteryTrendPoint = {
  periodKey: number;
  microSkillsRecorded: number;
  cumulativeMicroSkills: number;
};

export type GapHistoryEvent = {
  learningGapId: string;
  eventType: string;
  occurredAt: string;
};

export type InterventionOutcomePoint = {
  interventionId: string;
  learningGapId: string;
  status: string;
  createdAt: string;
};

export type StudentLongitudinalResponse = {
  organisationId: string;
  studentUserId: string;
  masteryTrend: MasteryTrendPoint[];
  gapHistory: GapHistoryEvent[];
  interventionOutcomes: InterventionOutcomePoint[];
};

export type StudentLongitudinalSummary = {
  studentUserId: string;
  cumulativeMicroSkills: number;
  interventionCount: number;
  gapEventCount: number;
};

export type OrganisationLongitudinalResponse = {
  organisationId: string;
  studentSummaries: StudentLongitudinalSummary[];
  schoolMasteryTrend: MasteryTrendPoint[];
};

async function analyticsRequest<T>(
  token: string,
  path: string
): Promise<T> {
  const response = await fetch(`${reportingApiUrl}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Analytics request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchStudentLongitudinal(
  token: string,
  organisationId: string,
  studentUserId: string
): Promise<StudentLongitudinalResponse> {
  return analyticsRequest<StudentLongitudinalResponse>(
    token,
    `/api/v1/analytics/organisations/${organisationId}/students/${encodeURIComponent(studentUserId)}/longitudinal`
  );
}

export function fetchOrganisationLongitudinal(
  token: string,
  organisationId: string
): Promise<OrganisationLongitudinalResponse> {
  return analyticsRequest<OrganisationLongitudinalResponse>(
    token,
    `/api/v1/analytics/organisations/${organisationId}/longitudinal`
  );
}

export function formatPeriodKey(periodKey: number): string {
  const year = Math.floor(periodKey / 100);
  const month = periodKey % 100;
  const date = new Date(year, month - 1, 1);
  return date.toLocaleDateString(undefined, { month: "short", year: "numeric" });
}
