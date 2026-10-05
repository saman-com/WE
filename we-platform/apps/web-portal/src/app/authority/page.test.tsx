import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AuthorityPolicyDashboardPage from "@/app/authority/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import {
  chartPointsForCountCells,
  chartValueForCountCell,
  formatCountCell,
  type CountCell,
  type PolicyTrendsResponse,
} from "@/lib/policy-dashboards";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const fetchPolicyTrends = vi.fn<() => Promise<PolicyTrendsResponse>>();
const fetchEquityAnalysis = vi.fn();
const fetchCurriculumEffectiveness = vi.fn();
const fetchInterventionImpact = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/policy-dashboards", async () => {
  const actual = await vi.importActual<typeof import("@/lib/policy-dashboards")>(
    "@/lib/policy-dashboards"
  );
  return {
    ...actual,
    fetchPolicyTrends: () => fetchPolicyTrends(),
    fetchEquityAnalysis: () => fetchEquityAnalysis(),
    fetchCurriculumEffectiveness: () => fetchCurriculumEffectiveness(),
    fetchInterventionImpact: () => fetchInterventionImpact(),
  };
});

const authority: UserProfile = {
  id: "authority-1",
  email: "authority@ministry.local",
  name: "Authority Officer",
  roles: ["EducationAuthorityOfficer"],
};

const visible = (value: number): CountCell => ({ value, suppressed: false });
const hidden = (): CountCell => ({ value: null, suppressed: true });

function renderPage() {
  return render(
    <I18nProvider>
      <AuthorityPolicyDashboardPage />
    </I18nProvider>
  );
}

describe("CountCell helpers", () => {
  it("formats suppressed cells with the provided label and skips them in chart series", () => {
    expect(formatCountCell(hidden(), "Hidden (fewer than 5 students)")).toBe(
      "Hidden (fewer than 5 students)"
    );
    expect(formatCountCell(visible(42), "Hidden (fewer than 5 students)")).toBe("42");
    expect(chartValueForCountCell(hidden())).toBeNull();
    expect(chartValueForCountCell(visible(12))).toBe(12);
    expect(
      chartPointsForCountCells(
        [
          { regionCode: "A", studentCount: visible(10) },
          { regionCode: "B", studentCount: hidden() },
        ],
        (row) => row.studentCount
      )
    ).toEqual([{ regionCode: "A", value: 10 }]);
  });
});

describe("Authority policy dashboard CountCell rendering", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(authority);
    fetchEquityAnalysis.mockReset().mockResolvedValue({ asOfDate: "2026-10-01", distributions: [] });
    fetchCurriculumEffectiveness.mockReset().mockResolvedValue({
      asOfDate: "2026-10-01",
      regions: [],
    });
    fetchInterventionImpact.mockReset().mockResolvedValue({
      asOfDate: "2026-10-01",
      regions: [],
    });
    fetchPolicyTrends.mockReset().mockResolvedValue({
      asOfDate: "2026-10-01",
      totalSchools: 2,
      totalStudents: hidden(),
      nationalAverageMasteryPercent: 70,
      regions: [
        {
          regionCode: "NORTH",
          schoolCount: 1,
          studentCount: visible(40),
          averageMasteryPercent: 72,
        },
        {
          regionCode: "SOUTH",
          schoolCount: 1,
          studentCount: hidden(),
          averageMasteryPercent: null,
        },
      ],
    });
  });

  afterEach(cleanup);

  it("renders suppressed cells as Hidden label and omits them from chart points", async () => {
    renderPage();

    expect(await screen.findByTestId("authority-total-students")).toHaveTextContent(
      "Hidden (fewer than 5 students)"
    );
    expect(screen.getByTestId("authority-region-NORTH")).toHaveTextContent("40");
    expect(screen.getByTestId("authority-region-SOUTH")).toHaveTextContent(
      "Hidden (fewer than 5 students)"
    );

    const chart = screen.getByTestId("authority-student-chart-points");
    expect(chart).toHaveTextContent("NORTH:40");
    expect(chart).not.toHaveTextContent("SOUTH");
  });
});
