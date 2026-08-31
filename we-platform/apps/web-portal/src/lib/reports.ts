const reportingApiUrl =
  process.env.NEXT_PUBLIC_REPORTING_API_URL ?? "http://localhost:8096";

export type ClassMasteryDistributionSection = {
  microSkillId: string;
  levelCounts: Record<string, number>;
  totalStudents: number;
  explanation: string;
  linkedEvidenceIds: string[];
};

export type ClassActiveGapSection = {
  gapId: string;
  microSkillId: string;
  severity: string;
  urgency: string;
  studentUserId: string;
  explanation: string;
  evidenceId: string;
};

export type AssessmentSummarySection = {
  assessmentId: string;
  title: string;
  status: string;
  submissionCount: number;
  reviewedCount: number;
  dueAt: string | null;
};

export type ClassProgressReportContent = {
  organisationId: string;
  classId: string;
  className: string;
  masteryDistribution: ClassMasteryDistributionSection[];
  activeLearningGaps: ClassActiveGapSection[];
  assessmentSummary: AssessmentSummarySection[];
  generatedAt: string;
};

export type LeadershipKpiSection = {
  totalStudents: number;
  totalClasses: number;
  activeInterventions: number;
  activeLearningGaps: number;
  studentsNeedingAttention: number;
  assessmentCompletionRate: number;
  masteryLevelCounts: Record<string, number>;
};

export type YearLevelSummarySection = {
  yearLevelId: string;
  yearLevelName: string;
  classCount: number;
  studentCount: number;
  activeInterventions: number;
  activeLearningGaps: number;
};

export type ClassComparisonSection = {
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

export type SchoolSummaryReportContent = {
  organisationId: string;
  organisationName: string;
  kpis: LeadershipKpiSection;
  yearLevels: YearLevelSummarySection[];
  classComparisons: ClassComparisonSection[];
  generatedAt: string;
};

export type ReportResponse = {
  id: string;
  reportType: string;
  organisationId: string;
  classId: string | null;
  requestedByUserId: string;
  generatedAt: string;
  content: ClassProgressReportContent | SchoolSummaryReportContent;
};

async function reportingRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${reportingApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Reporting request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function generateClassProgressReport(
  token: string,
  organisationId: string,
  classId: string
): Promise<ReportResponse> {
  return reportingRequest<ReportResponse>(
    token,
    `/api/v1/reports/organisations/${organisationId}/classes/${classId}/class-progress`,
    { method: "POST" }
  );
}

export function generateSchoolSummaryReport(
  token: string,
  organisationId: string
): Promise<ReportResponse> {
  return reportingRequest<ReportResponse>(
    token,
    `/api/v1/reports/organisations/${organisationId}/school-summary`,
    { method: "POST" }
  );
}

export async function downloadReportPdf(
  token: string,
  reportId: string,
  filename: string
): Promise<void> {
  const response = await fetch(`${reportingApiUrl}/api/v1/reports/${reportId}/pdf`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error(`PDF export failed (${response.status}).`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

export function isClassProgressReport(
  content: ClassProgressReportContent | SchoolSummaryReportContent
): content is ClassProgressReportContent {
  return "className" in content;
}
