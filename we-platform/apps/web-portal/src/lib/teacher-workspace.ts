import { type SchoolClass } from "@/lib/organisation";

const organisationApiUrl =
  process.env.NEXT_PUBLIC_ORGANISATION_API_URL ?? "http://localhost:8082";

export type ClassDashboardStudentSummary = {
  studentUserId: string;
  evidenceCount: number;
  latestActivityAt: string | null;
};

export type ClassDashboardAssessmentSummary = {
  id: string;
  title: string;
  status: string;
  dueAt: string | null;
  submissionCount: number;
  reviewedCount: number;
};

export type ClassDashboard = {
  class: SchoolClass;
  roster: ClassDashboardStudentSummary[];
  recentAssessments: ClassDashboardAssessmentSummary[];
};

async function teacherWorkspaceRequest<T>(
  token: string,
  path: string
): Promise<T> {
  const response = await fetch(`${organisationApiUrl}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Teacher workspace request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchClassDashboard(
  token: string,
  organisationId: string,
  classId: string
): Promise<ClassDashboard> {
  return teacherWorkspaceRequest<ClassDashboard>(
    token,
    `/api/v1/organisations/${organisationId}/classes/${classId}/dashboard`
  );
}
