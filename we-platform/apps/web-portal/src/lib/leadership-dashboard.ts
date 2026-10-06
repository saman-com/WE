const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type LeadershipKpiSummary = {
  totalStudents: number;
  totalClasses: number;
  activeInterventions: number;
  activeLearningGaps: number | null;
  studentsNeedingAttention: number | null;
  assessmentCompletionRate: number;
  masteryLevelCounts: Record<string, number>;
  studentsNeedingAttentionReason?: string | null;
};

export type YearLevelDashboardSummary = {
  yearLevelId: string;
  yearLevelName: string;
  classCount: number;
  studentCount: number;
  activeInterventions: number;
  activeLearningGaps: number | null;
};

export type ClassComparisonSummary = {
  classId: string;
  className: string;
  yearLevelId: string;
  yearLevelName: string;
  studentCount: number;
  activeInterventions: number;
  activeLearningGaps: number | null;
  studentsNeedingAttention: number | null;
  assessmentCompletionRate: number;
};

export type LeadershipDashboard = {
  organisationId: string;
  organisationName: string;
  kpis: LeadershipKpiSummary;
  yearLevels: YearLevelDashboardSummary[];
  classComparisons: ClassComparisonSummary[];
};

export type YearLevelLeadershipDashboard = {
  organisationId: string;
  yearLevelId: string;
  yearLevelName: string;
  kpis: LeadershipKpiSummary;
  classComparisons: ClassComparisonSummary[];
};

export type ClassLeadershipSummary = {
  class: {
    id: string;
    organisationId: string;
    yearLevelId: string;
    name: string;
    code: string;
    teacherUserIds: string[] | null;
    studentUserIds: string[] | null;
  };
  students: { userId: string }[];
  recentAssessments: {
    id: string;
    title: string;
    status: string;
    dueAt: string | null;
    submissionCount: number;
    reviewedCount: number;
  }[];
  activeInterventions: number;
  eiInsights: {
    masteryDistribution: unknown[];
    activeLearningGaps: unknown[];
    studentsNeedingAttention: unknown[];
  } | null;
};

export type LeadershipInterventionItem = {
  interventionId: string;
  studentUserId: string;
  learningGapId: string;
  assignedTeacherUserId: string;
  plannedActions: string;
  outcome: string | null;
  status: string;
  plannedStartAt: string | null;
  plannedEndAt: string | null;
  reviewAt: string | null;
  createdAt: string;
  classId: string;
  className: string;
  yearLevelId: string;
  yearLevelName: string;
  gapSeverity: string | null;
};

export type LeadershipInterventionMonitoring = {
  organisationId: string;
  interventions: LeadershipInterventionItem[];
};

export type LeadershipInterventionFilters = {
  yearLevelId?: string;
  classId?: string;
  severity?: string;
  status?: string;
};

async function leadershipRequest<T>(token: string, path: string): Promise<T> {
  const response = await fetch(`${organisationApiUrl}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Leadership dashboard request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchLeadershipDashboard(
  token: string,
  organisationId: string
): Promise<LeadershipDashboard> {
  return leadershipRequest<LeadershipDashboard>(
    token,
    `/api/v1/organisations/${organisationId}/leadership/dashboard`
  );
}

export function fetchYearLevelLeadershipDashboard(
  token: string,
  organisationId: string,
  yearLevelId: string
): Promise<YearLevelLeadershipDashboard> {
  return leadershipRequest<YearLevelLeadershipDashboard>(
    token,
    `/api/v1/organisations/${organisationId}/year-levels/${yearLevelId}/leadership/dashboard`
  );
}

export function fetchClassLeadershipSummary(
  token: string,
  organisationId: string,
  classId: string
): Promise<ClassLeadershipSummary> {
  return leadershipRequest<ClassLeadershipSummary>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/leadership/summary`
  );
}

function buildInterventionQuery(filters: LeadershipInterventionFilters): string {
  const params = new URLSearchParams();
  if (filters.yearLevelId) {
    params.set("yearLevelId", filters.yearLevelId);
  }
  if (filters.classId) {
    params.set("classId", filters.classId);
  }
  if (filters.severity) {
    params.set("severity", filters.severity);
  }
  if (filters.status) {
    params.set("status", filters.status);
  }
  const query = params.toString();
  return query ? `?${query}` : "";
}

export function fetchLeadershipInterventions(
  token: string,
  organisationId: string,
  filters: LeadershipInterventionFilters = {}
): Promise<LeadershipInterventionMonitoring> {
  return leadershipRequest<LeadershipInterventionMonitoring>(
    token,
    `/api/v1/organisations/${organisationId}/leadership/interventions${buildInterventionQuery(filters)}`
  );
}
