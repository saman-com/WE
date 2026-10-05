import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherClassDetailPage from "@/app/teacher/classes/[classId]/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { ClassEiInsights } from "@/lib/ei-insights";
import type { ClassDashboard } from "@/lib/teacher-workspace";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
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
});
