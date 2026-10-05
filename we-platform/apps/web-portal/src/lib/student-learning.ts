import type { Paged } from "@/lib/paging";

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
  hasMore: boolean;
  nextCursor: string | null;
};

export function fetchStudentProfile(
  token: string,
  studentUserId: string,
  options?: { pageSize?: number; cursor?: string | null; page?: number }
): Promise<StudentProfile> {
  const params = new URLSearchParams();
  if (options?.pageSize) {
    params.set("pageSize", String(options.pageSize));
  }
  if (options?.page) {
    params.set("page", String(options.page));
  }
  if (options?.cursor) {
    params.set("cursor", options.cursor);
  }
  const query = params.toString();
  return fetch(
    `${studentLearningApiUrl}/api/v1/students/${studentUserId}/profile${query ? `?${query}` : ""}`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  ).then(async (response) => {
    if (!response.ok) {
      throw new Error(`Student profile request failed (${response.status}).`);
    }
    const payload = (await response.json()) as StudentProfile & Partial<Paged<EvidenceTimelineEntry>>;
    return {
      studentUserId: payload.studentUserId,
      enrollments: payload.enrollments,
      evidenceTimeline: payload.evidenceTimeline,
      hasMore: payload.hasMore ?? false,
      nextCursor: payload.nextCursor ?? null,
    };
  });
}
