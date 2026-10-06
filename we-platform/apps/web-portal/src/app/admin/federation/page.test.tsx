import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import FederationAdminPage from "@/app/admin/federation/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ApiError } from "@/lib/api-error";
import type { UserProfile } from "@/lib/auth";
import { createFederationSchool, updateFederationPolicies } from "@/lib/federation";

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
    expect(screen.getByText("Shared curriculum: Enabled")).toBeInTheDocument();
    expect(screen.queryByText("shared-curriculum: enabled")).not.toBeInTheDocument();
    expect(screen.queryByText(tenantId)).not.toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i
    );
  });

  it("says the school code is already in use when create returns a duplicate", async () => {
    vi.mocked(createFederationSchool).mockRejectedValue(
      new ApiError("federation.duplicate_school_code", 409)
    );
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <FederationAdminPage />
      </I18nProvider>
    );

    await screen.findByText("North Federation School (DFED)");
    await user.click(screen.getAllByRole("button", { name: "Add a school" })[0]);
    await user.type(screen.getByLabelText("School name"), "QA School");
    await user.type(screen.getByLabelText("School code"), "DFED");
    await user.click(screen.getByRole("button", { name: "Create school" }));

    expect(await screen.findByRole("status")).toHaveTextContent(
      "That school code is already in use."
    );
    expect(screen.queryByText("The school was not added. Configuration could not be saved.")).not.toBeInTheDocument();
  });

  it("rejects a one-character policy value", async () => {
    vi.mocked(updateFederationPolicies).mockRejectedValue(
      new ApiError("federation.policy_value_too_short", 400)
    );
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <FederationAdminPage />
      </I18nProvider>
    );

    await screen.findByText("North Federation School (DFED)");
    await user.click(screen.getAllByRole("button", { name: "Policy" })[0]);
    await user.clear(screen.getByLabelText("Policy key"));
    await user.clear(screen.getByLabelText("Policy value"));
    await user.type(screen.getByLabelText("Policy key"), "qa-policy");
    await user.type(screen.getByLabelText("Policy value"), "x");
    await user.click(screen.getByRole("button", { name: "Save policy" }));

    expect(await screen.findByRole("status")).toHaveTextContent(
      "A policy value needs at least 2 characters."
    );
    expect(screen.queryByText("Federation policy updated.")).not.toBeInTheDocument();
  });
});
