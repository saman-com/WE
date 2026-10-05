import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import FederationAdminPage from "@/app/admin/federation/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const tenantId = "01a10e0a-1038-7cc4-aec9-7c08fe126deb";
const { schools } = vi.hoisted(() => ({
  schools: [
    {
      tenantId: "01a10e0a-1038-7cc4-aec9-7c08fe126deb",
      federationId: "fed-1",
      name: "North Federation School",
      code: "DFED",
      hasDefaultConfiguration: true,
      createdAt: "2026-10-06T00:00:00.000Z",
    },
  ],
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listDirectoryUsers: vi.fn().mockResolvedValue([
      { id: "admin-1", name: "Demo Admin", email: "admin@school.local", roles: ["SystemAdministrator"] },
    ]),
  };
});

vi.mock("@/lib/federation", () => ({
  listFederationSchools: vi.fn().mockResolvedValue(schools),
  fetchFederationMetrics: vi.fn().mockResolvedValue({
    totalSchools: 1,
    totalEnrollment: 120,
    averageProgressPercent: 68.5,
    schools: [],
  }),
  fetchFederationPolicies: vi.fn().mockResolvedValue([
    { policyKey: "shared-curriculum", policyValue: "enabled" },
  ]),
  createFederationSchool: vi.fn(),
  assignSchoolAdmin: vi.fn(),
  updateFederationPolicies: vi.fn(),
}));

describe("federation admin display", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "fed-admin",
      email: "federation@ministry.local",
      name: "Demo Federation Admin",
      roles: ["FederationAdmin"],
    } satisfies UserProfile);
  });

  afterEach(cleanup);

  it("shows the school name and code without the tenant id", async () => {
    render(
      <I18nProvider>
        <FederationAdminPage />
      </I18nProvider>
    );

    expect(await screen.findByText("North Federation School (DFED)")).toBeInTheDocument();
    expect(screen.queryByText(tenantId)).not.toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i
    );
  });
});
