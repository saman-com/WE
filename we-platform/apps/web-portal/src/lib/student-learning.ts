import { UnexpectedPageError } from "@/lib/paging";

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

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function parseStudentProfile(value: unknown): StudentProfile {
  if (
    !isRecord(value) ||
    typeof value.studentUserId !== "string" ||
    !Array.isArray(value.enrollments) ||
    !Array.isArray(value.evidenceTimeline) ||
    typeof value.hasMore !== "boolean" ||
    (value.nextCursor !== null && typeof value.nextCursor !== "string")
  ) {
    throw new UnexpectedPageError();
  }

  return {
    studentUserId: value.studentUserId,
    enrollments: value.enrollments as ClassEnrollmentSummary[],
    evidenceTimeline: value.evidenceTimeline as EvidenceTimelineEntry[],
    hasMore: value.hasMore,
    nextCursor: value.nextCursor,
  };
}

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
    return parseStudentProfile(await response.json());
  });
}
