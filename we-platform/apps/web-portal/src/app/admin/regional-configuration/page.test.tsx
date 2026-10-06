import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import RegionalConfigurationAdminPage from "@/app/admin/regional-configuration/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import { updateRegionalConfiguration, type RegionalConfiguration } from "@/lib/regional-configuration";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const fetchRegionalConfiguration = vi.fn<(token: string) => Promise<RegionalConfiguration>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/regional-configuration", () => ({
  fetchRegionalConfiguration: (token: string) => fetchRegionalConfiguration(token),
  updateRegionalConfiguration: vi.fn(),
}));

const admin: UserProfile = {
  id: "admin-1",
  email: "admin@school.local",
  name: "Demo Admin",
  roles: ["SystemAdministrator"],
};

function config(updatedAt: string): RegionalConfiguration {
  return {
    tenantId: "org-1",
    academicCalendar: { terms: [], holidays: [] },
    gradingScale: { name: "Default", levels: [] },
    assessmentModels: [],
    reportingTemplates: [],
    localeSettings: {
      languageCode: "en",
      regionCode: "US",
      dateFormat: "yyyy-MM-dd",
      timeZone: "UTC",
    },
    updatedAt,
    updatedByUserId: "",
  };
}

function renderPage() {
  return render(
    <I18nProvider>
      <RegionalConfigurationAdminPage />
    </I18nProvider>
  );
}

describe("regional configuration last updated", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(admin);
    fetchRegionalConfiguration.mockReset();
  });

  afterEach(cleanup);

  it("hides the timestamp until a real save exists", async () => {
    fetchRegionalConfiguration.mockResolvedValue(config("1970-01-01T00:00:00.000Z"));

    renderPage();

    expect(await screen.findByRole("button", { name: "Save configuration" })).toBeInTheDocument();
    expect(screen.queryByText(/Last updated/)).not.toBeInTheDocument();
  });

  it("shows the timestamp after a real save", async () => {
    fetchRegionalConfiguration.mockResolvedValue(config("2026-10-06T10:00:00.000Z"));

    renderPage();

    expect(await screen.findByText(/Last updated:/)).toBeInTheDocument();
  });

  it("rejects an overlapping grading level and keeps it off the scale", async () => {
    const user = userEvent.setup();
    fetchRegionalConfiguration.mockResolvedValue({
      ...config("1970-01-01T00:00:00.000Z"),
      gradingScale: {
        name: "Default",
        levels: [
          { label: "A", minScore: 85, maxScore: 100 },
          { label: "B", minScore: 70, maxScore: 84.99 },
        ],
      },
    });

    renderPage();

    await user.click((await screen.findAllByRole("button", { name: "Grading scale" }))[0]);
    const addLevel = screen.getByRole("button", { name: "Add level" });
    expect(addLevel.className).toContain("border");
    expect(addLevel.className).not.toContain("bg-[#1c1917]");
    expect(
      screen.getByText("Levels are saved when you save the configuration.")
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue("A")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Add a level" })).toBeInTheDocument();

    const labels = screen.getAllByLabelText("Label");
    const minimums = screen.getAllByLabelText("Minimum score");
    const maximums = screen.getAllByLabelText("Maximum score");
    await user.type(labels[labels.length - 1], "C");
    await user.clear(minimums[minimums.length - 1]);
    await user.type(minimums[minimums.length - 1], "80");
    await user.clear(maximums[maximums.length - 1]);
    await user.type(maximums[maximums.length - 1], "90");
    await user.click(screen.getByRole("button", { name: "Add level" }));

    expect(screen.getByRole("status")).toHaveTextContent("Level score ranges cannot overlap.");
    expect(screen.getAllByRole("button", { name: "Move up" })).toHaveLength(2);
    expect(updateRegionalConfiguration).not.toHaveBeenCalled();
  });
});
