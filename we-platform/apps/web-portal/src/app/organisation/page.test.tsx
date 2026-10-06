import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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

const listDirectoryUsers = vi.fn();
const createDirectoryUser = vi.fn();

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listDirectoryUsers: (token: string) => listDirectoryUsers(token),
    createDirectoryUser: (
      token: string,
      account: { name: string; email: string; password: string; role: string }
    ) => createDirectoryUser(token, account),
  };
});

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
    listDirectoryUsers.mockReset().mockResolvedValue([
      { id: "22222222-2222-2222-2222-222222222222", name: "Demo Student", email: "student@school.local", roles: ["Student"] },
    ]);
    createDirectoryUser.mockReset();
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

    const school = await screen.findByRole("option", { name: "Demo school" });
    expect(school).toHaveAttribute("dir", "auto");
    expect(school.querySelector("bdi")).toBeNull();
    expect(await screen.findByText("Year 11")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Add a year level" })).toBeInTheDocument();
    expect(screen.getByLabelText("Sort order")).toHaveValue(12);
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create the first class" })).not.toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-2222-2222-222222222222/)).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Add an account" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "System administrator" })).toBeInTheDocument();
  });

  it("creates an account with a password for the chosen role", async () => {
    const user = userEvent.setup();
    const created = {
      id: "new-1",
      name: "New Teacher",
      email: "new.teacher@school.local",
      roles: ["Teacher"],
    };
    let people = [
      { id: "22222222-2222-2222-2222-222222222222", name: "Demo Student", email: "student@school.local", roles: ["Student"] },
    ];
    listDirectoryUsers.mockImplementation(async () => people);
    createDirectoryUser.mockImplementation(async () => {
      people = [...people, created];
      return created;
    });

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    await screen.findByRole("heading", { name: "Add an account" });
    await user.type(screen.getByLabelText("Name"), "New Teacher");
    await user.type(screen.getByLabelText("Email"), "new.teacher@school.local");
    await user.type(screen.getByLabelText("Password"), "Password123!");
    await user.selectOptions(screen.getByLabelText("Role"), "Teacher");
    await user.click(screen.getByRole("button", { name: "Add account" }));

    expect(createDirectoryUser).toHaveBeenCalledWith("token", {
      name: "New Teacher",
      email: "new.teacher@school.local",
      password: "Password123!",
      role: "Teacher",
    });
    expect(await screen.findByText("New Teacher (new.teacher@school.local)")).toBeInTheDocument();
  });

  it("sends a signed-in teacher back to the teacher home", async () => {
    fetchProfile.mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    expect(
      await screen.findByText("Only system administrators can set up organisations.")
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to home" })).toHaveAttribute("href", "/teacher");
    expect(screen.queryByRole("link", { name: "Back to login" })).not.toBeInTheDocument();
    expect(listOrganisations).not.toHaveBeenCalled();
  });
});
