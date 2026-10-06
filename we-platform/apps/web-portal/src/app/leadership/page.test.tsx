import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import LeadershipDashboardPage from "@/app/leadership/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import { fetchLeadershipDashboard } from "@/lib/leadership-dashboard";

const { studentId, teacherId, gapId, organisationId, classId } = vi.hoisted(() => ({
  studentId: "22222222-2222-2222-2222-222222222222",
  teacherId: "11111111-1111-1111-1111-111111111111",
  gapId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  organisationId: "00000000-0000-4000-8000-000000000001",
  classId: "00000000-0000-4000-8000-000000000111",
}));

function personName(
  people: Array<{ id: string; name?: string; email?: string }>,
  userId: string,
  unknown: string
) {
  const match = people.find((person) => person.id === userId);
  return match?.name || match?.email || unknown;
}

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const loadPeople = vi.fn();
const loadLearningNames = vi.fn();
const loadGapLabels = vi.fn();
const fetchClassLeadershipSummary = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
  personName: (
    people: Array<{ id: string; name?: string; email?: string }>,
    userId: string,
    unknown: string
  ) => personName(people, userId, unknown),
}));

vi.mock("@/lib/display-names", () => ({
  loadPeople: (...args: unknown[]) => loadPeople(...args),
  loadLearningNames: (...args: unknown[]) => loadLearningNames(...args),
  loadGapLabels: (...args: unknown[]) => loadGapLabels(...args),
  learningLabel: (names: Record<string, string>, id: string, unknown: string) => names[id] || unknown,
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: vi.fn().mockResolvedValue([
    { id: organisationId, name: "Demo school", code: "DEMO" },
  ]),
}));

vi.mock("@/lib/leadership-dashboard", () => ({
  fetchLeadershipDashboard: vi.fn().mockResolvedValue({
    organisationId,
    organisationName: "Demo school",
    kpis: {
      totalStudents: 1,
      totalClasses: 1,
      activeInterventions: 1,
      activeLearningGaps: 1,
      studentsNeedingAttention: 0,
      assessmentCompletionRate: 1,
      masteryLevelCounts: {},
    },
    yearLevels: [],
    classComparisons: [
      {
        classId,
        className: "Year 11 Mathematics",
        yearLevelId: "year-11",
        yearLevelName: "Year 11",
        studentCount: 1,
        activeInterventions: 1,
        activeLearningGaps: 1,
        studentsNeedingAttention: 0,
        assessmentCompletionRate: 1,
      },
    ],
  }),
  fetchLeadershipInterventions: vi.fn().mockResolvedValue({
    organisationId,
    interventions: [
      {
        interventionId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        studentUserId: studentId,
        learningGapId: gapId,
        assignedTeacherUserId: teacherId,
        plannedActions: "Guided algebra practice",
        outcome: null,
        status: "Active",
        plannedStartAt: null,
        plannedEndAt: null,
        reviewAt: null,
        createdAt: "2026-10-01T00:00:00Z",
        classId,
        className: "Year 11 Mathematics",
        yearLevelId: "year-11",
        yearLevelName: "Year 11",
        gapSeverity: "High",
      },
    ],
  }),
  fetchYearLevelLeadershipDashboard: vi.fn(),
  fetchClassLeadershipSummary: (...args: unknown[]) => fetchClassLeadershipSummary(...args),
}));

vi.mock("@/lib/longitudinal-analytics", () => ({
  fetchOrganisationLongitudinal: vi.fn().mockResolvedValue({
    organisationId,
    studentSummaries: [
      {
        studentUserId: studentId,
        cumulativeMicroSkills: 2,
        interventionCount: 1,
        gapEventCount: 1,
      },
    ],
    schoolMasteryTrend: [],
  }),
}));

vi.mock("@/lib/effectiveness-analytics", () => ({
  fetchOrganisationEffectiveness: vi.fn().mockRejectedValue(new Error("unavailable")),
}));

vi.mock("@/lib/reports", () => ({
  generateSchoolSummaryReport: vi.fn(),
  downloadReportPdf: vi.fn(),
}));

describe("leadership dashboard", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "55555555-5555-5555-5555-555555555555",
      email: "leader@school.local",
      name: "Demo Leader",
      roles: ["SchoolLeader"],
    });
    loadPeople.mockReset().mockResolvedValue([
      { id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] },
      { id: teacherId, name: "Demo Teacher", email: "teacher@school.local", roles: ["Teacher"] },
    ]);
    loadLearningNames.mockReset().mockResolvedValue({});
    loadGapLabels.mockReset().mockResolvedValue({ [gapId]: "Read an equation" });
    fetchClassLeadershipSummary.mockReset().mockResolvedValue({
      class: {
        id: classId,
        organisationId,
        yearLevelId: "year-11",
        name: "Year 11 Mathematics",
        code: "11MAT",
        teacherUserIds: [teacherId],
        studentUserIds: [studentId],
      },
      students: [{ userId: studentId }],
      recentAssessments: [],
      activeInterventions: 1,
      eiInsights: null,
    });
  });

  afterEach(cleanup);

  it("shows names and never a GUID", async () => {
    const user = userEvent.setup();
    const { container } = render(
      <I18nProvider>
        <LeadershipDashboardPage />
      </I18nProvider>
    );

    expect(await screen.findByRole("link", { name: "Demo Student" })).toBeInTheDocument();
    expect(screen.getByText("Demo Teacher")).toBeInTheDocument();
    expect(screen.getByText("Read an equation")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Year 11 Mathematics (Year 11)" }));
    expect(await screen.findByRole("link", { name: "View profile — Demo Student" })).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });

  it("shows Not available instead of 0 when EI metrics fail", async () => {
    const unavailable = {
      organisationId,
      organisationName: "Demo school",
      kpis: {
        totalStudents: 1,
        totalClasses: 1,
        activeInterventions: 1,
        activeLearningGaps: null,
        studentsNeedingAttention: null,
        assessmentCompletionRate: 1,
        masteryLevelCounts: {},
      },
      yearLevels: [],
      classComparisons: [
        {
          classId,
          className: "Year 11 Mathematics",
          yearLevelId: "year-11",
          yearLevelName: "Year 11",
          studentCount: 1,
          activeInterventions: 1,
          activeLearningGaps: null,
          studentsNeedingAttention: null,
          assessmentCompletionRate: 1,
        },
      ],
    };
    // React may invoke the load effect twice in development.
    vi.mocked(fetchLeadershipDashboard).mockResolvedValueOnce(unavailable);
    vi.mocked(fetchLeadershipDashboard).mockResolvedValueOnce(unavailable);

    render(
      <I18nProvider>
        <LeadershipDashboardPage />
      </I18nProvider>
    );

    const gaps = await screen.findByText("Active learning gaps");
    expect(gaps.parentElement).toHaveTextContent("Not available");
    expect(gaps.parentElement?.textContent ?? "").not.toContain("0");

    const attention = screen.getByText("Students needing attention");
    expect(attention.parentElement).toHaveTextContent("Not available");
    expect(attention.parentElement?.textContent ?? "").not.toContain("0");
  });

  it("labels students needing attention in English and Arabic", async () => {
    const dashboard = {
      organisationId,
      organisationName: "Demo school",
      kpis: {
        totalStudents: 1,
        totalClasses: 1,
        activeInterventions: 1,
        activeLearningGaps: 1,
        studentsNeedingAttention: 1,
        assessmentCompletionRate: 0.5,
        masteryLevelCounts: {},
        studentsNeedingAttentionReason: "high-severity-gap",
      },
      yearLevels: [],
      classComparisons: [],
    };
    vi.mocked(fetchLeadershipDashboard).mockResolvedValue(dashboard);

    const { unmount } = render(
      <I18nProvider>
        <LeadershipDashboardPage />
      </I18nProvider>
    );
    const attention = await screen.findByText("Students needing attention");
    expect(attention.parentElement).toHaveTextContent("High-severity gap");
    unmount();

    localStorage.setItem("we_locale", "ar");
    render(
      <I18nProvider>
        <LeadershipDashboardPage />
      </I18nProvider>
    );
    expect(await screen.findByText("فجوة عالية الخطورة")).toBeInTheDocument();
    expect(screen.getByText("طلاب يحتاجون إلى اهتمام")).toBeInTheDocument();
  });
});
