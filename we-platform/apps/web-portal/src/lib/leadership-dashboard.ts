const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type LeadershipKpiSummary = {
  totalStudents: number;
  totalClasses: number;
  activeInterventions: number;
  activeLearningGaps: number;
  studentsNeedingAttention: number;
  assessmentCompletionRate: number;
  masteryLevelCounts: Record<string, number>;
};

export type YearLevelDashboardSummary = {
  yearLevelId: string;
  yearLevelName: string;
  classCount: number;
  studentCount: number;
  activeInterventions: number;
  activeLearningGaps: number;
};

export type ClassComparisonSummary = {
  classId: string;
  className: string;
  yearLevelId: string;
  yearLevelName: string;
  studentCount: number;
  activeInterventions: number;
  activeLearningGaps: number;
  studentsNeedingAttention: number;
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
