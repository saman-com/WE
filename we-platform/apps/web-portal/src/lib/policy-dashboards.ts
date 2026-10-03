const nationalReportingApiUrl =
  process.env.NEXT_PUBLIC_NATIONAL_REPORTING_API_URL ?? "http://localhost:8100";

export type RegionTrendMetric = {
  regionCode: string;
  schoolCount: number;
  studentCount: number;
  averageMasteryPercent: number;
};

export type PolicyTrendsResponse = {
  asOfDate: string;
  totalSchools: number;
  totalStudents: number;
  nationalAverageMasteryPercent: number;
  regions: RegionTrendMetric[];
};

export type EquityMasteryDistribution = {
  regionCode: string;
  demographicDimension: string;
  demographicCategory: string;
  averageMasteryPercent: number;
  sampleSize: number;
};

export type EquityAnalysisResponse = {
  asOfDate: string;
  distributions: EquityMasteryDistribution[];
};

export type RegionCurriculumEffectiveness = {
  regionCode: string;
  curriculumCode: string;
  subjectCode: string;
  masteryRatePercent: number;
  coveragePercent: number;
  schoolsReporting: number;
};

export type CurriculumEffectivenessComparisonResponse = {
  asOfDate: string;
  regions: RegionCurriculumEffectiveness[];
};

export type RegionInterventionImpact = {
  regionCode: string;
  interventionType: string;
  totalCount: number;
  successfulCount: number;
  successRatePercent: number;
  averageGrowthPercent: number;
};

export type InterventionImpactResponse = {
  asOfDate: string;
  regions: RegionInterventionImpact[];
};

async function policyRequest<T>(token: string, path: string): Promise<T> {
  const response = await fetch(`${nationalReportingApiUrl}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Policy dashboard request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchPolicyTrends(token: string): Promise<PolicyTrendsResponse> {
  return policyRequest<PolicyTrendsResponse>(token, "/api/v1/policy-dashboards/trends");
}

export function fetchEquityAnalysis(token: string): Promise<EquityAnalysisResponse> {
  return policyRequest<EquityAnalysisResponse>(token, "/api/v1/policy-dashboards/equity");
}

export function fetchCurriculumEffectiveness(
  token: string
): Promise<CurriculumEffectivenessComparisonResponse> {
  return policyRequest<CurriculumEffectivenessComparisonResponse>(
    token,
    "/api/v1/policy-dashboards/curriculum-effectiveness"
  );
}

export function fetchInterventionImpact(
  token: string
): Promise<InterventionImpactResponse> {
  return policyRequest<InterventionImpactResponse>(
    token,
    "/api/v1/policy-dashboards/intervention-impact"
  );
}

export function formatPercent(value: number): string {
  return `${Math.round(value)}%`;
}
