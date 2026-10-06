import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherClassDetailPage from "@/app/teacher/classes/[classId]/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { ClassEiInsights } from "@/lib/ei-insights";
import type { ClassDashboard } from "@/lib/teacher-workspace";

const studentId = "22222222-2222-2222-2222-222222222222";
const skillId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const evidenceId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function personName(
  people: Array<{ id: string; name?: string; email?: string }>,
  userId: string,
  unknown: string
) {
  const match = people.find((person) => person.id === userId);
  return match?.name || match?.email || unknown;
}

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const loadPeople = vi.fn();
const loadLearningNames = vi.fn();
const fetchClassDashboard = vi.fn<
  (token: string, organisationId: string, classId: string) => Promise<ClassDashboard>
>();
const fetchClassEiInsights = vi.fn<
  (token: string, organisationId: string, classId: string) => Promise<ClassEiInsights>
>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
  useParams: () => ({ classId: "c-maths" }),
  useSearchParams: () => new URLSearchParams("organisationId=org-1"),
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
  learningLabel: (names: Record<string, string>, id: string, unknown: string) => names[id] || unknown,
}));

vi.mock("@/lib/teacher-workspace", () => ({
  fetchClassDashboard: (token: string, organisationId: string, classId: string) =>
    fetchClassDashboard(token, organisationId, classId),
}));

vi.mock("@/lib/ei-insights", () => ({
  fetchClassEiInsights: (token: string, organisationId: string, classId: string) =>
    fetchClassEiInsights(token, organisationId, classId),
}));

vi.mock("@/lib/ei-summaries", () => ({
  requestLessonSummaryDraft: vi.fn(),
  finalizeAiSummaryAudit: vi.fn(),
}));

vi.mock("@/lib/reports", () => ({
  generateClassProgressReport: vi.fn(),
  downloadReportPdf: vi.fn(),
  isClassProgressReport: vi.fn(),
}));

vi.mock("@/lib/interventions", () => ({
  buildCreateInterventionFromGapUrl: vi.fn(() => "/teacher/interventions/new"),
}));

const teacher: UserProfile = {
  id: "teacher-chen",
  email: "chen@school.local",
  name: "Ms Chen",
  roles: ["Teacher"],
};

const emptyDashboard: ClassDashboard = {
  class: {
    id: "c-maths",
    name: "Year 11 Mathematics",
    code: "11MAT",
    organisationId: "org-1",
    yearLevelId: "year-11",
    teacherUserIds: [teacher.id],
    studentUserIds: ["student-1"],
  },
  roster: [],
  recentAssessments: [],
};

const emptyInsights: ClassEiInsights = {
  organisationId: "org-1",
  classId: "c-maths",
  masteryDistribution: [],
  activeLearningGaps: [],
  recentDiagnosticTrends: [],
  studentsNeedingAttention: [],
};

function renderPage() {
  return render(
    <I18nProvider>
      <TeacherClassDetailPage />
    </I18nProvider>
  );
}

describe("Teacher class insights empty states (UX-001 §9)", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(teacher);
    loadPeople.mockReset().mockResolvedValue([]);
    loadLearningNames.mockReset().mockResolvedValue({});
    fetchClassDashboard.mockReset().mockResolvedValue(emptyDashboard);
    fetchClassEiInsights.mockReset().mockResolvedValue(emptyInsights);
  });

  afterEach(cleanup);

  it("shows empty Educational Intelligence copy when insights have no rows", async () => {
    renderPage();

    expect(await screen.findByRole("heading", { name: "Educational Intelligence" })).toBeInTheDocument();
    expect(
      screen.getByText("No mastery data yet. Insights appear after approved evidence is analysed.")
    ).toBeInTheDocument();
    expect(screen.getByText("No active learning gaps identified.")).toBeInTheDocument();
    expect(screen.getByText("No recent diagnostic trends.")).toBeInTheDocument();
    expect(screen.getByText("No students flagged for attention.")).toBeInTheDocument();
  });

  it("still shows empty insights when the EI request fails", async () => {
    fetchClassEiInsights.mockRejectedValue(new Error("EI unavailable"));

    renderPage();

    expect(await screen.findByRole("heading", { name: "Educational Intelligence" })).toBeInTheDocument();
    expect(screen.getByText("No active learning gaps identified.")).toBeInTheDocument();
    expect(screen.getByText("No students flagged for attention.")).toBeInTheDocument();
  });

  it("shows names and never a GUID", async () => {
    loadPeople.mockResolvedValue([{ id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] }]);
    loadLearningNames.mockResolvedValue({ [skillId]: "Read an equation" });
    fetchClassDashboard.mockResolvedValue({
      ...emptyDashboard,
      roster: [{ studentUserId: studentId, evidenceCount: 2, latestActivityAt: "2026-10-01T00:00:00Z" }],
    });
    fetchClassEiInsights.mockResolvedValue({
      ...emptyInsights,
      masteryDistribution: [
        {
          microSkillId: skillId,
          levelCounts: { Developing: 1 },
          totalStudents: 1,
          explanation: "Still building fluency.",
          linkedEvidenceIds: [evidenceId],
        },
      ],
      activeLearningGaps: [
        {
          gapId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          microSkillId: skillId,
          severity: "High",
          urgency: "High",
          studentUserId: studentId,
          explanation: "Needs another example.",
          evidenceId,
        },
      ],
      recentDiagnosticTrends: [
        {
          microSkillId: skillId,
          status: "Developing",
          occurrenceCount: 1,
          latestAt: "2026-10-02T00:00:00Z",
          explanation: "Recent checks are uneven.",
          evidenceId,
        },
      ],
      studentsNeedingAttention: [
        {
          studentUserId: studentId,
          reason: "Repeated struggle",
          explanation: "Ask for a worked example.",
          evidenceId,
        },
      ],
    });

    const { container } = renderPage();

    expect(await screen.findAllByText("Demo Student")).not.toHaveLength(0);
    expect(screen.getAllByText("Micro-skill: Read an equation").length).toBeGreaterThan(0);
    expect(screen.getByText("Linked evidence: 1")).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });
});
