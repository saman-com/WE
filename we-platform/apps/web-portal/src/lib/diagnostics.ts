const diagnosticApiUrl =
  process.env.NEXT_PUBLIC_DIAGNOSTIC_API_URL ?? "http://localhost:8088";

export type MicroSkillDiagnostic = {
  id: string;
  evidenceId: string;
  assessmentId: string;
  microSkillId: string;
  status: "Mastered" | "Developing" | "Struggling";
  mark: number;
  reason: string;
  createdAt: string;
};

export type StudentDiagnostics = {
  studentUserId: string;
  diagnostics: MicroSkillDiagnostic[];
};

export function fetchStudentDiagnostics(
  token: string,
  studentUserId: string
): Promise<StudentDiagnostics> {
  return fetch(`${diagnosticApiUrl}/api/v1/diagnostics/students/${studentUserId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student diagnostics request failed (${response.status}).`);
    }
    return response.json() as Promise<StudentDiagnostics>;
  });
}
