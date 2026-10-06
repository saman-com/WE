import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import CreateInterventionPage from "@/app/teacher/students/[studentUserId]/interventions/new/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const studentId = "22222222-2222-2222-2222-222222222222";
const skillId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const gapId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

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
const fetchStudentGaps = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
  useParams: () => ({ studentUserId: studentId }),
  useSearchParams: () =>
    new URLSearchParams(
      `organisationId=00000000-0000-4000-8000-000000000001&learningGapId=${gapId}`
    ),
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

vi.mock("@/lib/gaps", () => ({
  fetchStudentGaps: (...args: unknown[]) => fetchStudentGaps(...args),
}));

vi.mock("@/lib/interventions", async () => {
  const actual = await vi.importActual<typeof import("@/lib/interventions")>("@/lib/interventions");
  return {
    ...actual,
    createIntervention: vi.fn(),
  };
});

describe("new intervention page", () => {
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
    fetchStudentGaps.mockReset().mockResolvedValue({
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
    });
  });

  afterEach(cleanup);

  it("shows the student and skill names and never a GUID", async () => {
    const { container } = render(
      <I18nProvider>
        <CreateInterventionPage />
      </I18nProvider>
    );

    expect(await screen.findByText(/Micro-skill: Read an equation/)).toBeInTheDocument();
    expect(screen.getByText("Demo Student")).toBeInTheDocument();
    expect((screen.getByLabelText("Planned actions") as HTMLTextAreaElement).value).toContain(
      "Read an equation"
    );
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });
});
