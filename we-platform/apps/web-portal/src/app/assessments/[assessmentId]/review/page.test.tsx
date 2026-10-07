import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AssessmentReviewPage from "@/app/assessments/[assessmentId]/review/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ApiError } from "@/lib/api-error";
import { getAssessment } from "@/lib/assessment";
import type { UserProfile } from "@/lib/auth";

const { studentUserId, microSkillId, router } = vi.hoisted(() => ({
  studentUserId: "22222222-2222-2222-2222-222222222222",
  microSkillId: "00000000-0000-4000-8000-000000001001",
  router: { replace: vi.fn(), push: vi.fn() },
}));

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => router,
  useParams: () => ({ assessmentId: "assessment-1" }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listDirectoryUsers: vi.fn().mockResolvedValue([
      {
        id: studentUserId,
        name: "Demo Student",
        email: "student@school.local",
        roles: ["Student"],
      },
    ]),
  };
});

vi.mock("@/lib/assessment", () => ({
  getAssessment: vi.fn().mockResolvedValue({
    id: "assessment-1",
    organisationId: "org-1",
    classId: "class-1",
    title: "Algebra sheet",
    instructions: null,
    dueAt: null,
    status: "Published",
    publishedAt: null,
    learningObjectiveIds: ["objective-1"],
    microSkillIds: [microSkillId],
    createdByTeacherUserId: "teacher-1",
    createdAt: "2026-10-06T00:00:00.000Z",
    updatedAt: "2026-10-06T00:00:00.000Z",
  }),
  listSubmissions: vi.fn().mockResolvedValue([
    {
      id: "submission-1",
      assessmentId: "assessment-1",
      studentUserId,
      responses: "x = 4",
      status: "submitted",
      submittedAt: "2026-10-06T09:27:23.000Z",
      isLate: false,
    },
  ]),
  requestAiFeedbackDraft: vi.fn(),
  finalizeAiFeedbackAudit: vi.fn(),
}));

vi.mock("@/lib/evidence", () => ({
  listEvidenceForAssessment: vi.fn().mockResolvedValue({
    items: [
      {
        id: "evidence-1",
        assessmentId: "assessment-1",
        submissionId: "submission-1",
        studentUserId,
        title: "Algebra sheet",
        status: "approved",
        microSkillMarks: [{ microSkillId, mark: 4, feedback: "Clear working." }],
        approvedByTeacherUserId: "teacher-1",
        approvedAt: "2026-10-06T10:00:00.000Z",
      },
    ],
    hasMore: false,
    nextCursor: null,
  }),
  approveEvidence: vi.fn(),
}));

vi.mock("@/lib/curriculum", async () => {
  const actual = await vi.importActual<typeof import("@/lib/curriculum")>("@/lib/curriculum");
  return {
    ...actual,
    listCurricula: vi.fn().mockResolvedValue([{ id: "curriculum-1" }]),
    getCurriculumTree: vi.fn().mockResolvedValue({
      id: "curriculum-1",
      subjects: [
        {
          units: [
            {
              learningObjectives: [
                {
                  microSkills: [{ id: microSkillId, name: "Read an equation" }],
                },
              ],
            },
          ],
        },
      ],
    }),
  };
});

describe("assessment review labels", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    } satisfies UserProfile);
  });

  afterEach(cleanup);

  it("labels the student and skill by name and translates the status", async () => {
    render(
      <I18nProvider>
        <AssessmentReviewPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo Student")).toBeInTheDocument();
    expect(screen.getByText("Read an equation")).toBeInTheDocument();
    expect(screen.getByText("Approved")).toBeInTheDocument();
    expect(screen.getByText("Approved").className).not.toMatch(/uppercase/);
    expect(screen.queryByText(studentUserId)).not.toBeInTheDocument();
    expect(screen.queryByText(microSkillId)).not.toBeInTheDocument();
    expect(screen.queryByText("APPROVED")).not.toBeInTheDocument();
  });

  it("shows the Arabic denial after the locale has loaded", async () => {
    localStorage.setItem("we_locale", "ar");
    fetchProfile.mockResolvedValue({
      id: "admin-1",
      email: "admin@school.local",
      name: "Demo Admin",
      roles: ["SystemAdministrator"],
    });

    render(
      <I18nProvider>
        <AssessmentReviewPage />
      </I18nProvider>
    );

    expect(await screen.findByText("يمكن للمعلمين فقط اعتماد الأدلة.")).toBeInTheDocument();
    expect(screen.queryByText("Only teachers can approve evidence.")).not.toBeInTheDocument();
  });

  it("shows a translated not-found message when the assessment returns 404", async () => {
    vi.mocked(getAssessment).mockRejectedValueOnce(new ApiError("not_found", 404));

    render(
      <I18nProvider>
        <AssessmentReviewPage />
      </I18nProvider>
    );

    expect(await screen.findByText("This assessment was not found.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to assessments" })).toHaveAttribute(
      "href",
      "/assessments"
    );
    expect(screen.queryByText("Loading submissions...")).not.toBeInTheDocument();
  });

  it("shows the Arabic not-found message for a missing assessment", async () => {
    localStorage.setItem("we_locale", "ar");
    vi.mocked(getAssessment).mockRejectedValueOnce(new ApiError("not_found", 404));

    render(
      <I18nProvider>
        <AssessmentReviewPage />
      </I18nProvider>
    );

    expect(await screen.findByText("لم يُعثر على هذا التقييم.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "العودة إلى التقييمات" })).toHaveAttribute(
      "href",
      "/assessments"
    );
    expect(screen.queryByText("جارٍ تحميل التسليمات...")).not.toBeInTheDocument();
  });
});
