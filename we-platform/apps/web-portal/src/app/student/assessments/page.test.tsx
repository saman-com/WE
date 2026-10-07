import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import StudentAssessmentsPage from "@/app/student/assessments/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";
import type { Assessment, AssessmentSubmission } from "@/lib/assessment";
import type { Organisation, SchoolClass } from "@/lib/organisation";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listOrganisations = vi.fn<(token: string) => Promise<Organisation[]>>();
const listClasses = vi.fn<(token: string, organisationId: string) => Promise<SchoolClass[]>>();
const listAssessments = vi.fn();
const getMySubmission = vi.fn<(token: string, assessmentId: string) => Promise<AssessmentSubmission | null>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
  listClasses: (token: string, organisationId: string) => listClasses(token, organisationId),
}));

vi.mock("@/lib/assessment", () => ({
  listAssessments: (...args: unknown[]) => listAssessments(...args),
  getMySubmission: (token: string, assessmentId: string) => getMySubmission(token, assessmentId),
  submitAssessment: vi.fn(),
}));

const assessment: Assessment = {
  id: "sheet",
  organisationId: "org-1",
  classId: "class-1",
  title: "Algebra sheet",
  instructions: null,
  dueAt: "2026-09-28T10:00:00Z",
  status: "Published",
  publishedAt: "2026-09-20T00:00:00Z",
  learningObjectiveIds: [],
  microSkillIds: [],
  createdByTeacherUserId: "teacher-1",
  createdAt: "2026-09-20T00:00:00Z",
  updatedAt: "2026-09-20T00:00:00Z",
};

describe("student assessments submitted line", () => {
  beforeEach(() => {
    replace.mockReset();
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    fetchProfile.mockReset().mockResolvedValue({
      id: "student-1",
      email: "student@school.local",
      name: "Demo Student",
      roles: ["Student"],
    });
    listOrganisations.mockReset().mockResolvedValue([{ id: "org-1", name: "Demo school", code: "DEMO" }]);
    listClasses.mockReset().mockResolvedValue([
      {
        id: "class-1",
        organisationId: "org-1",
        yearLevelId: "year-11",
        name: "Year 11 Mathematics",
        code: "11MAT",
        teacherUserIds: [],
        studentUserIds: ["student-1"],
      },
    ]);
    listAssessments.mockReset().mockResolvedValue({
      items: [assessment],
      hasMore: false,
      nextCursor: null,
    });
    getMySubmission.mockReset().mockResolvedValue({
      id: "submission-1",
      assessmentId: assessment.id,
      studentUserId: "student-1",
      responses: "x = 4",
      status: "submitted",
      isLate: false,
      submittedAt: "2026-10-06T09:27:23.000Z",
      createdAt: "2026-10-06T09:27:23.000Z",
      updatedAt: "2026-10-06T09:27:23.000Z",
    });
  });

  afterEach(cleanup);

  it("puts the submitted date outside the Arabic sentence and translates the status", async () => {
    render(
      <I18nProvider>
        <StudentAssessmentsPage />
      </I18nProvider>
    );

    const line = await screen.findByText(/تم التسليم/);
    const date = line.querySelector("bdi");
    expect(date).not.toBeNull();
    expect(date).not.toHaveTextContent("تم التسليم");
    expect(line).not.toHaveTextContent(/\bsubmitted\b/);
    expect(line).toHaveTextContent("تم التسليم");
  });

  it("sends a signed-in teacher to the teacher home", async () => {
    fetchProfile.mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });

    render(
      <I18nProvider>
        <StudentAssessmentsPage />
      </I18nProvider>
    );

    await vi.waitFor(() => {
      expect(replace).toHaveBeenCalledWith("/teacher");
    });
    expect(screen.queryByText("This page is for students only.")).not.toBeInTheDocument();
    expect(listOrganisations).not.toHaveBeenCalled();
  });
});
