import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "@/app/dashboard/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import { listOrganisations } from "@/lib/organisation";
import type { UserProfile } from "@/lib/auth";

const { studentUserId, listDirectoryUsers } = vi.hoisted(() => ({
  studentUserId: "22222222-2222-2222-2222-222222222222",
  listDirectoryUsers: vi.fn(),
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
    listDirectoryUsers: (token: string) => listDirectoryUsers(token),
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

describe("dashboard profile", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    listDirectoryUsers.mockReset().mockResolvedValue([
      {
        id: studentUserId,
        name: "Demo Student",
        email: "student@school.local",
        roles: ["Student"],
      },
    ]);
    fetchProfile.mockReset().mockResolvedValue({
      id: "55555555-5555-5555-5555-555555555555",
      email: "leader@school.local",
      name: "Demo School Leader",
      roles: ["SchoolLeader", "Student"],
    } satisfies UserProfile);
  });

  afterEach(cleanup);

  it("shows the person and hides ids, role codes, and joined nav links", async () => {
    render(
      <I18nProvider>
        <DashboardPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo School Leader")).toBeInTheDocument();
    expect(screen.getByText("leader@school.local")).toBeInTheDocument();
    expect(screen.queryByText("55555555-5555-5555-5555-555555555555")).not.toBeInTheDocument();
    expect(screen.queryByText("SchoolLeader")).not.toBeInTheDocument();
    expect(screen.queryByText(studentUserId)).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Demo Student/ })).toBeInTheDocument();

    const workspace = screen.getByRole("link", { name: "Student workspace" });
    const assessments = screen.getByRole("link", { name: "My assessments" });
    expect(workspace.className).toContain("block");
    expect(assessments.className).toContain("block");
  });

  it("does not load organisations for a parent", async () => {
    vi.mocked(listOrganisations).mockClear();
    fetchProfile.mockResolvedValue({
      id: "33333333-3333-3333-3333-333333333333",
      email: "parent@school.local",
      name: "Demo Parent",
      roles: ["Parent"],
    });

    render(
      <I18nProvider>
        <DashboardPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo Parent")).toBeInTheDocument();
    expect(listOrganisations).not.toHaveBeenCalled();
    expect(listDirectoryUsers).not.toHaveBeenCalled();
  });

  it("does not load the user directory for an authority officer", async () => {
    fetchProfile.mockResolvedValue({
      id: "66666666-6666-6666-6666-666666666666",
      email: "authority@school.local",
      name: "Demo Authority",
      roles: ["EducationAuthorityOfficer"],
    });

    render(
      <I18nProvider>
        <DashboardPage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo Authority")).toBeInTheDocument();
    expect(listDirectoryUsers).not.toHaveBeenCalled();
  });

  it("keeps the profile name outside the Arabic label", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    fetchProfile.mockResolvedValue({
      id: "22222222-2222-2222-2222-222222222222",
      email: "student@school.local",
      name: "Amina Student",
      roles: ["Student"],
    });

    render(
      <I18nProvider>
        <DashboardPage />
      </I18nProvider>
    );

    const name = await screen.findByText("Amina Student");
    expect(name.tagName).toBe("BDI");
    expect(name.parentElement).not.toHaveTextContent("الاسم");
    expect(screen.getByText("الاسم:")).toBeInTheDocument();
  });
});
