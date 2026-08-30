const masteryApiUrl =
  process.env.NEXT_PUBLIC_MASTERY_API_URL ?? "http://localhost:8090";

export type MasteryRecord = {
  id: string;
  microSkillId: string;
  masteryLevel: "NotStarted" | "Developing" | "Proficient" | "Mastered";
  weightedAverage: number;
  confidenceScore: number;
  evidenceCount: number;
  explanation: string;
  calculatedAt: string;
};

export type StudentMastery = {
  studentUserId: string;
  records: MasteryRecord[];
};

export function fetchStudentMastery(
  token: string,
  studentUserId: string
): Promise<StudentMastery> {
  return fetch(`${masteryApiUrl}/api/v1/mastery/students/${studentUserId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student mastery request failed (${response.status}).`);
    }
    return response.json() as Promise<StudentMastery>;
  });
}
