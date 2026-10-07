import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "@/app/dashboard/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
  listDirectoryUsers: vi.fn().mockResolvedValue([]),
  personName: (people: Array<{ id: string; name?: string }>, userId: string, unknown: string) =>
    people.find((person) => person.id === userId)?.name || unknown,
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: vi.fn().mockResolvedValue([]),
  listClasses: vi.fn().mockResolvedValue([]),
}));

describe("dashboard profile", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    fetchProfile.mockReset().mockResolvedValue({
      id: "22222222-2222-2222-2222-222222222222",
      email: "student@school.local",
      name: "Demo Student",
      roles: ["Student"],
    });
  });

  afterEach(cleanup);

  it("keeps the profile name outside the Arabic label", async () => {
    render(
      <I18nProvider>
        <DashboardPage />
      </I18nProvider>
    );

    const name = await screen.findByText("Demo Student");
    expect(name.tagName).toBe("BDI");
    expect(name.parentElement).not.toHaveTextContent("الاسم");
    expect(screen.getByText("الاسم:")).toBeInTheDocument();
  });
});
