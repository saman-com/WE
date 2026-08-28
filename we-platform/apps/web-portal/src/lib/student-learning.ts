const studentLearningApiUrl =
  process.env.NEXT_PUBLIC_STUDENT_LEARNING_API_URL ?? "http://localhost:8084";

export type ClassEnrollmentSummary = {
  organisationId: string;
  classId: string;
  className: string;
  classCode: string;
  enrolledAt: string;
};

export type EvidenceTimelineEntry = {
  id: string;
  title: string;
  recordedAt: string;
};

export type StudentProfile = {
  studentUserId: string;
  enrollments: ClassEnrollmentSummary[];
  evidenceTimeline: EvidenceTimelineEntry[];
};

export function fetchStudentProfile(
  token: string,
  studentUserId: string
): Promise<StudentProfile> {
  return fetch(`${studentLearningApiUrl}/api/v1/students/${studentUserId}/profile`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student profile request failed (${response.status}).`);
    }
    return response.json() as Promise<StudentProfile>;
  });
}
