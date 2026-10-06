import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import OrganisationSetupPage from "@/app/organisation/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ApiError } from "@/lib/api-error";
import type { UserProfile } from "@/lib/auth";
import { createYearLevel, listEnrollments, listParentChildren, listTeachers } from "@/lib/organisation";

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
const updateDirectoryUser = vi.fn();
const deactivateDirectoryUser = vi.fn();
const reactivateDirectoryUser = vi.fn();
const resetDirectoryUserPassword = vi.fn();

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
    updateDirectoryUser: (
      token: string,
      userId: string,
      account: { name: string; role: string }
    ) => updateDirectoryUser(token, userId, account),
    deactivateDirectoryUser: (token: string, userId: string) => deactivateDirectoryUser(token, userId),
    reactivateDirectoryUser: (token: string, userId: string) => reactivateDirectoryUser(token, userId),
    resetDirectoryUserPassword: (token: string, userId: string, password: string) =>
      resetDirectoryUserPassword(token, userId, password),
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
    updateDirectoryUser.mockReset();
    deactivateDirectoryUser.mockReset();
    reactivateDirectoryUser.mockReset();
    resetDirectoryUserPassword.mockReset();
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
    expect(screen.getByRole("button", { name: /^Edit$/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create the first class" })).not.toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-2222-2222-222222222222/)).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Accounts" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Add an account" })).not.toBeInTheDocument();
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

    const [accountsTab] = await screen.findAllByRole("button", { name: "Accounts" });
    await user.click(accountsTab);
    await screen.findByRole("heading", { name: "Add an account" });
    expect(screen.getByRole("option", { name: "System administrator" })).toBeInTheDocument();
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

  it("edits, deactivates, reactivates, and resets an account", async () => {
    const user = userEvent.setup();
    const student = {
      id: "22222222-2222-2222-2222-222222222222",
      name: "Demo Student",
      email: "student@school.local",
      roles: ["Student"],
      active: true,
    };
    let people = [student];
    listDirectoryUsers.mockImplementation(async () => people);
    updateDirectoryUser.mockImplementation(async (_token, _userId, account) => {
      people = [{ ...student, name: account.name, roles: [account.role] }];
      return people[0];
    });
    deactivateDirectoryUser.mockImplementation(async () => {
      people = [{ ...people[0], active: false }];
      return people[0];
    });
    reactivateDirectoryUser.mockImplementation(async () => {
      people = [{ ...people[0], active: true }];
      return people[0];
    });
    resetDirectoryUserPassword.mockResolvedValue(people[0]);

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    const [accountsTab] = await screen.findAllByRole("button", { name: "Accounts" });
    await user.click(accountsTab);
    await user.click(await screen.findByRole("button", { name: "Edit Demo Student" }));
    const editForm = screen.getByRole("form", { name: "Edit Demo Student" });
    await user.clear(within(editForm).getByLabelText("Name"));
    await user.type(within(editForm).getByLabelText("Name"), "Renamed Student");
    await user.selectOptions(within(editForm).getByLabelText("Role"), "Teacher");
    await user.click(within(editForm).getByRole("button", { name: "Save" }));

    expect(updateDirectoryUser).toHaveBeenCalledWith("token", student.id, {
      name: "Renamed Student",
      role: "Teacher",
    });
    expect(await screen.findByText("Renamed Student (student@school.local)")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Deactivate Renamed Student" }));
    expect(deactivateDirectoryUser).toHaveBeenCalledWith("token", student.id);
    expect(await screen.findByText("Inactive")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Reactivate Renamed Student" }));
    expect(reactivateDirectoryUser).toHaveBeenCalledWith("token", student.id);
    expect(screen.queryByText("Inactive")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Reset password for Renamed Student" }));
    const resetForm = screen.getByRole("form", { name: "Reset password for Renamed Student" });
    await user.type(within(resetForm).getByLabelText("New password"), "NewPassword1");
    await user.click(within(resetForm).getByRole("button", { name: "Save" }));
    expect(resetDirectoryUserPassword).toHaveBeenCalledWith("token", student.id, "NewPassword1");
  });

  it("does not offer deactivation or another role on the signed-in administrator", async () => {
    const user = userEvent.setup();
    listDirectoryUsers.mockResolvedValue([
      {
        id: "admin-1",
        name: "Demo Admin",
        email: "admin@school.local",
        roles: ["SystemAdministrator"],
        active: true,
      },
    ]);

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    const [accountsTab] = await screen.findAllByRole("button", { name: "Accounts" });
    await user.click(accountsTab);
    expect(await screen.findByRole("button", { name: "Edit Demo Admin" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reset password for Demo Admin" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Deactivate Demo Admin/ })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Edit Demo Admin" }));
    const form = screen.getByRole("form", { name: "Edit Demo Admin" });
    expect(within(form).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "System administrator",
    ]);
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

  it("names a duplicate year level instead of a generic save failure", async () => {
    vi.mocked(createYearLevel).mockRejectedValue(
      new ApiError("organisation.duplicate_year_name", 409)
    );
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    await screen.findByText("Year 11");
    await user.type(screen.getByLabelText("Year level name"), "Year 11");
    await user.click(screen.getByRole("button", { name: "Add year level" }));

    expect(await screen.findByText("That year level already exists.")).toBeInTheDocument();
    expect(screen.queryByText("Could not save that change.")).not.toBeInTheDocument();
  });

  it("shows only the selected tab's heading", async () => {
    const user = userEvent.setup();
    vi.mocked(listTeachers).mockResolvedValue([]);
    vi.mocked(listEnrollments).mockResolvedValue([]);
    vi.mocked(listParentChildren).mockResolvedValue([]);

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    const tabs = [
      "Year levels",
      "Classes",
      "Staff",
      "Students",
      "Parent links",
      "Accounts",
    ] as const;
    await screen.findByRole("option", { name: "Demo school" });

    const seen: string[] = [];
    for (const button of screen.getAllByRole("button")) {
      const name = button.textContent ?? "";
      if ((tabs as readonly string[]).includes(name) && !seen.includes(name)) {
        seen.push(name);
      }
    }
    expect(seen).toEqual([...tabs]);

    for (const tab of tabs) {
      const [tabButton] = screen.getAllByRole("button", { name: tab });
      await user.click(tabButton);
      expect(screen.getByRole("heading", { name: tab })).toBeInTheDocument();
      for (const other of tabs) {
        if (other === tab) {
          continue;
        }
        expect(screen.queryByRole("heading", { name: other })).not.toBeInTheDocument();
      }
    }
  });

  it("filters the accounts list by name or email", async () => {
    const user = userEvent.setup();
    listDirectoryUsers.mockResolvedValue([
      {
        id: "22222222-2222-2222-2222-222222222222",
        name: "Demo Student",
        email: "student@school.local",
        roles: ["Student"],
      },
      {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Demo Teacher",
        email: "teacher@school.local",
        roles: ["Teacher"],
      },
    ]);

    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    const [accountsTab] = await screen.findAllByRole("button", { name: "Accounts" });
    await user.click(accountsTab);
    expect(screen.getByText("Demo Student (student@school.local)")).toBeInTheDocument();
    expect(screen.getByText("Demo Teacher (teacher@school.local)")).toBeInTheDocument();

    const filter = screen.getByLabelText("Filter by name or email");
    await user.type(filter, "teacher@");
    expect(screen.queryByText("Demo Student (student@school.local)")).not.toBeInTheDocument();
    expect(screen.getByText("Demo Teacher (teacher@school.local)")).toBeInTheDocument();

    await user.clear(filter);
    await user.type(filter, "Demo S");
    expect(screen.getByText("Demo Student (student@school.local)")).toBeInTheDocument();
    expect(screen.queryByText("Demo Teacher (teacher@school.local)")).not.toBeInTheDocument();

    await user.clear(filter);
    await user.type(filter, "nobody");
    expect(screen.getByText("No accounts match.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Add an account" })).toBeInTheDocument();
  });
});
