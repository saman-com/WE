import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AssessmentsPage from "@/app/assessments/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

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
});
