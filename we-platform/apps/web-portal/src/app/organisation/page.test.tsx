import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import OrganisationSetupPage from "@/app/organisation/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listOrganisations = vi.fn();
const listYearLevels = vi.fn();
const listClasses = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
  listYearLevels: (token: string, organisationId: string) => listYearLevels(token, organisationId),
  listClasses: (token: string, organisationId: string) => listClasses(token, organisationId),
  createOrganisation: vi.fn(),
  createYearLevel: vi.fn(),
  createClass: vi.fn(),
  updateYearLevel: vi.fn(),
  deleteYearLevel: vi.fn(),
  updateClass: vi.fn(),
  deleteClass: vi.fn(),
  assignTeacher: vi.fn(),
  unassignTeacher: vi.fn(),
  enrollStudent: vi.fn(),
  unenrollStudent: vi.fn(),
  listTeachers: vi.fn(),
  listEnrollments: vi.fn(),
  listParentChildren: vi.fn(),
  linkParentChild: vi.fn(),
  unlinkParentChild: vi.fn(),
}));

describe("organisation manage view", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue({
      id: "admin-1",
      email: "admin@school.local",
      name: "Demo Admin",
      roles: ["SystemAdministrator"],
    } satisfies UserProfile);
    listOrganisations.mockReset().mockResolvedValue([
      { id: "org-1", name: "Demo school", code: "DEMO" },
    ]);
    listYearLevels.mockReset().mockResolvedValue([
      { id: "year-1", organisationId: "org-1", name: "Year 11", sortOrder: 11 },
    ]);
    listClasses.mockReset().mockResolvedValue([
      {
        id: "class-1",
        organisationId: "org-1",
        yearLevelId: "year-1",
        name: "Year 11 Mathematics",
        code: "11MAT",
      },
    ]);
  });

  afterEach(cleanup);

  it("lists the school and year level, and hides the first-time setup", async () => {
    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    expect(await screen.findByRole("option", { name: "Demo school" })).toBeInTheDocument();
    expect(await screen.findByText("Year 11")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create the first class" })).not.toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-2222-2222-222222222222/)).not.toBeInTheDocument();
  });
});
