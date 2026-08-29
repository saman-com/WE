const gapApiUrl =
  process.env.NEXT_PUBLIC_GAP_API_URL ?? "http://localhost:8089";

export type LearningGap = {
  id: string;
  evidenceId: string;
  assessmentId: string;
  microSkillId: string;
  learningObjectiveId: string | null;
  expectedMastery: string;
  actualMastery: string;
  mark: number;
  severity: "Low" | "Medium" | "High";
  urgency: "Low" | "Medium" | "High";
  explanation: string;
  createdAt: string;
};

export type StudentLearningGaps = {
  studentUserId: string;
  gaps: LearningGap[];
};

export function fetchStudentGaps(
  token: string,
  studentUserId: string
): Promise<StudentLearningGaps> {
  return fetch(`${gapApiUrl}/api/v1/gaps/students/${studentUserId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student learning gaps request failed (${response.status}).`);
    }
    return response.json() as Promise<StudentLearningGaps>;
  });
}
