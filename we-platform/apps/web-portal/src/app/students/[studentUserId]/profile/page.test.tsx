import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import StudentProfilePage from "@/app/students/[studentUserId]/profile/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const { studentId, skillId, gapId } = vi.hoisted(() => ({
  studentId: "22222222-2222-2222-2222-222222222222",
  skillId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  gapId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
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

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
  useParams: () => ({ studentUserId: studentId }),
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
  replaceVisibleIds: (
    text: string,
    people: Array<{ id: string; name?: string }>,
    names: Record<string, string>,
    _unknownPerson: string,
    unknownSkill: string
  ) =>
    text.replace(
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi,
      (id) => names[id] || people.find((person) => person.id === id)?.name || unknownSkill
    ),
}));

vi.mock("@/lib/student-learning", () => ({
  fetchStudentProfile: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    enrollments: [
      {
        organisationId: "00000000-0000-4000-8000-000000000001",
        classId: "00000000-0000-4000-8000-000000000111",
        className: "Year 11 Mathematics",
        classCode: "11MAT",
        enrolledAt: "2026-02-01T00:00:00Z",
      },
    ],
    evidenceTimeline: [],
    hasMore: false,
    nextCursor: null,
  }),
}));

vi.mock("@/lib/diagnostics", () => ({
  fetchStudentDiagnostics: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    diagnostics: [
      {
        id: "ffffffff-ffff-4fff-8fff-ffffffffffff",
        evidenceId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        assessmentId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        microSkillId: skillId,
        status: "Developing",
        mark: 2,
        reason: "Still building fluency.",
        createdAt: "2026-10-01T00:00:00Z",
      },
    ],
  }),
}));

vi.mock("@/lib/gaps", () => ({
  fetchStudentGaps: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    gaps: [
      {
        id: gapId,
        evidenceId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        assessmentId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
        microSkillId: skillId,
        learningObjectiveId: null,
        expectedMastery: "Proficient",
        actualMastery: "Developing",
        mark: 2,
        severity: "High",
        urgency: "High",
        explanation: "Needs another example.",
        createdAt: "2026-10-01T00:00:00Z",
      },
    ],
  }),
}));

vi.mock("@/lib/mastery", () => ({
  fetchStudentMastery: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    records: [
      {
        id: "99999999-9999-4999-8999-999999999999",
        microSkillId: skillId,
        masteryLevel: "Developing",
        weightedAverage: 2,
        confidenceScore: 0.5,
        evidenceCount: 1,
        explanation: "Early evidence.",
        calculatedAt: "2026-10-01T00:00:00Z",
      },
    ],
  }),
}));

vi.mock("@/lib/interventions", async () => {
  const actual = await vi.importActual<typeof import("@/lib/interventions")>("@/lib/interventions");
  return {
    ...actual,
  fetchStudentInterventions: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    interventions: [
      {
        id: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        organisationId: "00000000-0000-4000-8000-000000000001",
        studentUserId: studentId,
        learningGapId: gapId,
        assignedTeacherUserId: "11111111-1111-1111-1111-111111111111",
        plannedActions: "Guided algebra practice",
        notes: "",
        outcome: null,
        status: "Active",
        plannedStartAt: null,
        plannedEndAt: null,
        reviewAt: null,
        createdAt: "2026-10-01T00:00:00Z",
        updatedAt: "2026-10-02T00:00:00Z",
      },
    ],
  }),
  };
});

vi.mock("@/lib/longitudinal-analytics", () => ({
  fetchStudentLongitudinal: vi.fn().mockResolvedValue({
    organisationId: "00000000-0000-4000-8000-000000000001",
    studentUserId: studentId,
    masteryTrend: [],
    gapHistory: [
      { learningGapId: gapId, eventType: "Opened", occurredAt: "2026-10-01T00:00:00Z" },
    ],
    interventionOutcomes: [
      {
        interventionId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        learningGapId: gapId,
        status: "Active",
        createdAt: "2026-10-02T00:00:00Z",
      },
    ],
  }),
}));

vi.mock("@/lib/ei-summaries", () => ({
  requestProgressReportDraft: vi.fn(),
  finalizeAiSummaryAudit: vi.fn(),
}));

describe("student profile", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "11111111-1111-1111-1111-111111111111",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });
    loadPeople.mockReset().mockResolvedValue([
      { id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] },
    ]);
    loadLearningNames.mockReset().mockResolvedValue({ [skillId]: "Read an equation" });
  });

  afterEach(cleanup);

  it("shows names and never a GUID", async () => {
    const { container } = render(
      <I18nProvider>
        <StudentProfilePage />
      </I18nProvider>
    );

    expect(await screen.findByText("Demo Student")).toBeInTheDocument();
    expect(screen.getAllByText(/Read an equation/).length).toBeGreaterThan(0);
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });
});
