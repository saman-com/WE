const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type StudentWorkspaceAssessment = {
  id: string;
  organisationId: string;
  classId: string;
  className: string;
  title: string;
  dueAt: string | null;
  learningObjectiveIds: string[];
  hasSubmitted: boolean;
  submittedAt: string | null;
};

export type StudentWorkspaceFeedbackMark = {
  microSkillId: string;
  mark: number;
  feedback: string;
};

export type StudentWorkspaceFeedback = {
  evidenceId: string;
  assessmentId: string;
  title: string;
  approvedAt: string;
  microSkillMarks: StudentWorkspaceFeedbackMark[];
};

export type StudentWorkspaceTimelineEntry = {
  id: string;
  title: string;
  recordedAt: string;
};

export type StudentWorkspace = {
  studentUserId: string;
  assessments: StudentWorkspaceAssessment[];
  feedback: StudentWorkspaceFeedback[];
  timeline: StudentWorkspaceTimelineEntry[];
};

export function fetchStudentWorkspace(
  token: string,
  studentUserId: string
): Promise<StudentWorkspace> {
  return fetch(`${organisationApiUrl}/api/v1/students/${studentUserId}/workspace`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student workspace request failed (${response.status}).`);
    }
    return response.json() as Promise<StudentWorkspace>;
  });
}
