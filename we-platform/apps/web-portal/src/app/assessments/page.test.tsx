import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AssessmentsPage from "@/app/assessments/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";
import { listAssessments, type Assessment } from "@/lib/assessment";
import { getCurriculumTree, listCurricula } from "@/lib/curriculum";

const { studentUserId } = vi.hoisted(() => ({
  studentUserId: "22222222-2222-2222-2222-222222222222",
}));
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
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

vi.mock("@/lib/organisation", () => ({
  listOrganisations: vi.fn().mockResolvedValue([
    { id: "org-1", name: "Demo School", code: "DEMO" },
  ]),
  listClasses: vi.fn().mockResolvedValue([
    {
      id: "class-1",
      organisationId: "org-1",
      yearLevelId: "year-1",
      name: "Year 11 Mathematics",
      code: "11MAT",
      studentUserIds: [studentUserId],
    },
  ]),
}));

vi.mock("@/lib/assessment", () => ({
  listAssessments: vi.fn().mockResolvedValue({ items: [], hasMore: false, nextCursor: null }),
  createAssessment: vi.fn(),
  publishAssessment: vi.fn(),
}));

vi.mock("@/lib/curriculum", () => ({
  listCurricula: vi.fn().mockResolvedValue([]),
  getCurriculumTree: vi.fn(),
}));

describe("assessment class roster", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    } satisfies UserProfile);
    vi.mocked(listCurricula).mockReset().mockResolvedValue([]);
    vi.mocked(getCurriculumTree).mockReset();
    vi.mocked(listAssessments).mockReset().mockResolvedValue({
      items: [],
      hasMore: false,
      nextCursor: null,
    });
  });

  afterEach(cleanup);

  it("shows the enrolled student name instead of the user id", async () => {
    render(
      <I18nProvider>
        <AssessmentsPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo Student")).toBeInTheDocument();
    expect(screen.queryByText(studentUserId)).not.toBeInTheDocument();
  });

  it("shows draft and published in the active language", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    const assessment = (status: string, title: string): Assessment => ({
      id: title,
      organisationId: "org-1",
      classId: "class-1",
      title,
      instructions: null,
      dueAt: null,
      status,
      publishedAt: null,
      learningObjectiveIds: [],
      microSkillIds: status === "Published" ? ["skill-1"] : [],
      createdByTeacherUserId: "teacher-1",
      createdAt: "2026-10-06T00:00:00Z",
      updatedAt: "2026-10-06T00:00:00Z",
    });
    vi.mocked(listAssessments).mockResolvedValue({
      items: [assessment("Draft", "Quiz"), assessment("Published", "Exam")],
      hasMore: false,
      nextCursor: null,
    });

    render(
      <I18nProvider>
        <AssessmentsPage />
      </I18nProvider>
    );

    expect(await screen.findByText("مسودة")).toBeInTheDocument();
    expect(screen.getByText("منشور")).toBeInTheDocument();
    expect(screen.queryByText("DRAFT")).not.toBeInTheDocument();
    expect(screen.queryByText("PUBLISHED")).not.toBeInTheDocument();
  });

  it("opens on the curriculum named like the selected class", async () => {
    vi.mocked(listCurricula).mockResolvedValue([
      {
        id: "cambridge",
        organisationId: "org-1",
        name: "Cambridge Science",
        version: "2027",
        status: "Published",
        regionCode: null,
        scope: "Organisation",
        parentCurriculumId: null,
      },
      {
        id: "math",
        organisationId: "org-1",
        name: "Year 11 Mathematics",
        version: "1.0",
        status: "Published",
        regionCode: null,
        scope: "Organisation",
        parentCurriculumId: null,
      },
    ]);
    vi.mocked(getCurriculumTree).mockImplementation(async (_token, curriculumId) => ({
      id: curriculumId,
      organisationId: "org-1",
      name: curriculumId === "math" ? "Year 11 Mathematics" : "Cambridge Science",
      version: curriculumId === "math" ? "1.0" : "2027",
      status: "Published",
      regionCode: null,
      scope: "Organisation",
      parentCurriculumId: null,
      subjects: curriculumId === "math"
        ? [{
            id: "subject-1",
            name: "Mathematics",
            code: "MATH",
            sortOrder: 1,
            sourceNodeId: null,
            isOverridden: false,
            units: [{
              id: "unit-1",
              name: "Algebra",
              sortOrder: 1,
              sourceNodeId: null,
              isOverridden: false,
              topics: [],
              learningObjectives: [{
                id: "objective-1",
                title: "Solve linear equations",
                sortOrder: 1,
                sourceNodeId: null,
                isOverridden: false,
                microSkills: [
                  { id: "skill-read", name: "Read an equation", sortOrder: 1, sourceNodeId: null, isOverridden: false },
                  { id: "skill-solve", name: "Solve linear equations", sortOrder: 2, sourceNodeId: null, isOverridden: false },
                ],
              }],
            }],
          }]
        : [],
    }));

    render(
      <I18nProvider>
        <AssessmentsPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Read an equation")).toBeInTheDocument();
    expect(screen.getAllByText("Solve linear equations").length).toBeGreaterThan(0);
    expect(screen.getByRole("combobox", { name: "Curriculum (for LO / micro-skill links)" })).toHaveValue("math");
  });
});
