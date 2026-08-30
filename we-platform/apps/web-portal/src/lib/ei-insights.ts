const eiApiUrl =
  process.env.NEXT_PUBLIC_EI_API_URL ?? "http://localhost:8091";

export type ClassMicroSkillMasteryDistribution = {
  microSkillId: string;
  levelCounts: Record<string, number>;
  totalStudents: number;
  explanation: string;
  linkedEvidenceIds: string[];
};

export type ClassActiveLearningGap = {
  gapId: string;
  microSkillId: string;
  severity: "Low" | "Medium" | "High";
  urgency: "Low" | "Medium" | "High";
  studentUserId: string;
  explanation: string;
  evidenceId: string;
};

export type ClassDiagnosticTrend = {
  microSkillId: string;
  status: "Mastered" | "Developing" | "Struggling";
  occurrenceCount: number;
  latestAt: string;
  explanation: string;
  evidenceId: string;
};

export type StudentNeedingAttention = {
  studentUserId: string;
  reason: string;
  explanation: string;
  evidenceId: string | null;
};

export type ClassEiInsights = {
  organisationId: string;
  classId: string;
  masteryDistribution: ClassMicroSkillMasteryDistribution[];
  activeLearningGaps: ClassActiveLearningGap[];
  recentDiagnosticTrends: ClassDiagnosticTrend[];
  studentsNeedingAttention: StudentNeedingAttention[];
};

export function fetchClassEiInsights(
  token: string,
  organisationId: string,
  classId: string
): Promise<ClassEiInsights> {
  return fetch(
    `${eiApiUrl}/api/v1/ei/organisations/${organisationId}/classes/${classId}/insights`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  ).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Class EI insights request failed (${response.status}).`);
    }
    return response.json() as Promise<ClassEiInsights>;
  });
}
