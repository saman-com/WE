import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import OrganisationSetupPage from "@/app/organisation/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listOrganisations = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
  createOrganisation: vi.fn(),
  createYearLevel: vi.fn(),
  createClass: vi.fn(),
  assignTeacher: vi.fn(),
  enrollStudent: vi.fn(),
}));

describe("organisation intro", () => {
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
  });

  afterEach(cleanup);

  it("does not print the seed student id", async () => {
    render(
      <I18nProvider>
        <OrganisationSetupPage />
      </I18nProvider>
    );

    expect(
      await screen.findByText(
        "Create a school, add a year level and class, assign a teacher, and enroll a student."
      )
    ).toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-2222-2222-222222222222/)).not.toBeInTheDocument();
    expect(await screen.findByText("Demo school")).toBeInTheDocument();
  });
});
