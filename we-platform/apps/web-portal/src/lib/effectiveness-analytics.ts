const reportingApiUrl =
  process.env.NEXT_PUBLIC_REPORTING_API_URL ?? "http://localhost:8096";

export type CurriculumEffectivenessItem = {
  subjectId: string;
  subjectName: string;
  unitId: string;
  unitName: string;
  masteryRate: number;
  totalMicroSkills: number;
  masteredMicroSkills: number;
  isUnderperforming: boolean;
};

export type InterventionEffectivenessItem = {
  interventionType: string;
  totalCount: number;
  successfulCount: number;
  successRate: number;
};

export type OrganisationEffectivenessResponse = {
  organisationId: string;
  curriculumEffectiveness: CurriculumEffectivenessItem[];
  interventionEffectiveness: InterventionEffectivenessItem[];
};

export function fetchOrganisationEffectiveness(
  token: string,
  organisationId: string
): Promise<OrganisationEffectivenessResponse> {
  return fetch(
    `${reportingApiUrl}/api/v1/analytics/organisations/${organisationId}/effectiveness`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  ).then((response) => {
    if (!response.ok) {
      throw new Error(`Effectiveness request failed (${response.status}).`);
    }
    return response.json() as Promise<OrganisationEffectivenessResponse>;
  });
}

export function formatRate(rate: number): string {
  return `${Math.round(rate * 100)}%`;
}

export function formatInterventionType(type: string): string {
  return type.replace(/([A-Z])/g, " $1").trim();
}
