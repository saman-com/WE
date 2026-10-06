import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import CurriculumPage from "@/app/curriculum/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listOrganisations = vi.fn();
const listCurricula = vi.fn();
const getCurriculumTree = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
}));

vi.mock("@/lib/curriculum", () => ({
  listCurricula: (token: string, organisationId: string) => listCurricula(token, organisationId),
  getCurriculumTree: (token: string, curriculumId: string) => getCurriculumTree(token, curriculumId),
  createCurriculum: vi.fn(),
  inheritCurriculum: vi.fn(),
  createSubject: vi.fn(),
  createUnit: vi.fn(),
  createTopic: vi.fn(),
  createLearningObjective: vi.fn(),
  createMicroSkill: vi.fn(),
  updateUnit: vi.fn(),
  updateLearningObjective: vi.fn(),
}));

describe("curriculum tree", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "admin-1",
      email: "admin@school.local",
      name: "Demo Admin",
      roles: ["SystemAdministrator"],
    } satisfies UserProfile);
    listOrganisations.mockReset().mockResolvedValue([
      { id: "org-1", name: "Demo school", code: "DEMO" },
    ]);
    listCurricula.mockReset().mockResolvedValue([
      {
        id: "cur-1",
        organisationId: "org-1",
        name: "Year 11 Mathematics",
        version: "1.0",
        status: "Active",
        regionCode: null,
        scope: "School",
        parentCurriculumId: null,
      },
    ]);
    getCurriculumTree.mockReset().mockResolvedValue({
      id: "cur-1",
      organisationId: "org-1",
      name: "Year 11 Mathematics",
      version: "1.0",
      status: "Active",
      regionCode: null,
      scope: "School",
      parentCurriculumId: null,
      subjects: [
        {
          id: "sub-1",
          name: "Mathematics",
          code: "MATH",
          sortOrder: 1,
          sourceNodeId: null,
          isOverridden: false,
          units: [
            {
              id: "unit-1",
              name: "Algebra",
              sortOrder: 1,
              sourceNodeId: null,
              isOverridden: false,
              topics: [],
              learningObjectives: [],
            },
          ],
        },
      ],
    });
  });

  afterEach(cleanup);

  it("shows the curriculum tree and keeps add behind its own tab", async () => {
    render(
      <I18nProvider>
        <CurriculumPage />
      </I18nProvider>
    );

    expect(await screen.findByRole("button", { name: /Mathematics \(MATH\)/ })).toBeInTheDocument();
    const curriculum = screen.getByRole("option", { name: "Year 11 Mathematics" });
    expect(curriculum).toHaveAttribute("dir", "auto");
    expect(curriculum.querySelector("bdi")).toBeNull();
    expect(screen.getAllByRole("button", { name: "Add" }).length).toBeGreaterThan(0);
    expect(screen.queryByLabelText("Subject")).not.toBeInTheDocument();
  });

  it("starts the subject fields empty", async () => {
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <CurriculumPage />
      </I18nProvider>
    );

    await user.click((await screen.findAllByRole("button", { name: "Add" }))[0]);
    expect(screen.getByLabelText("Subject")).toHaveValue("");
    expect(screen.getByLabelText("Subject code")).toHaveValue("");
  });
});
