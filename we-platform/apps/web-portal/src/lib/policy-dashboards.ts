const nationalReportingApiUrl =
  process.env.NEXT_PUBLIC_NATIONAL_REPORTING_API_URL ?? "http://localhost:8100";

/** Matches NationalReportingService.Application.CountCell JSON shape. */
export type CountCell = {
  value: number | null;
  suppressed: boolean;
};

export const NATIONAL_MINIMUM_GROUP_SIZE = 5;

export type RegionTrendMetric = {
  regionCode: string;
  schoolCount: number;
  studentCount: CountCell;
  averageMasteryPercent: number | null;
};

export type PolicyTrendsResponse = {
  asOfDate: string;
  totalSchools: number;
  totalStudents: CountCell;
  nationalAverageMasteryPercent: number | null;
  regions: RegionTrendMetric[];
};

export type EquityMasteryDistribution = {
  regionCode: string;
  demographicDimension: string;
  demographicCategory: string;
  averageMasteryPercent: number | null;
  sampleSize: CountCell;
};

export type EquityAnalysisResponse = {
  asOfDate: string;
  distributions: EquityMasteryDistribution[];
};

export type RegionCurriculumEffectiveness = {
  regionCode: string;
  curriculumCode: string;
  subjectCode: string;
  masteryRatePercent: number | null;
  coveragePercent: number | null;
  schoolsReporting: CountCell;
};

export type CurriculumEffectivenessComparisonResponse = {
  asOfDate: string;
  regions: RegionCurriculumEffectiveness[];
};

export type RegionInterventionImpact = {
  regionCode: string;
  interventionType: string;
  totalCount: CountCell;
  successfulCount: CountCell;
  successRatePercent: number | null;
  averageGrowthPercent: number | null;
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

export function formatPercent(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) {
    return "—";
  }
  return `${Math.round(value)}%`;
}

export function isCountSuppressed(cell: CountCell | null | undefined): boolean {
  return !!cell?.suppressed;
}

/**
 * Chart series helper: omit suppressed cells (return null) so callers skip
 * the point instead of plotting 0.
 */
export function chartValueForCountCell(cell: CountCell | null | undefined): number | null {
  if (!cell || cell.suppressed || cell.value == null) {
    return null;
  }
  return cell.value;
}

export function formatCountCell(
  cell: CountCell | null | undefined,
  suppressedLabel: string
): string {
  if (!cell || cell.suppressed || cell.value == null) {
    return suppressedLabel;
  }
  return String(cell.value);
}

/** Build chart points, dropping suppressed counts. */
export function chartPointsForCountCells<T extends { regionCode: string }>(
  rows: T[],
  pick: (row: T) => CountCell
): Array<{ regionCode: string; value: number }> {
  const points: Array<{ regionCode: string; value: number }> = [];
  for (const row of rows) {
    const value = chartValueForCountCell(pick(row));
    if (value == null) {
      continue;
    }
    points.push({ regionCode: row.regionCode, value });
  }
  return points;
}
