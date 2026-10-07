import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import ParentWorkspacePage from "@/app/parent/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";
import { fetchChildProgress } from "@/lib/parent-workspace";

const { studentId, skillId, organisationId } = vi.hoisted(() => ({
  studentId: "22222222-2222-2222-2222-222222222222",
  skillId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  organisationId: "00000000-0000-4000-8000-000000000001",
}));

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listParentChildren = vi.fn();
const loadLearningNames = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listParentChildren: (...args: unknown[]) => listParentChildren(...args),
  };
});

vi.mock("@/lib/display-names", () => ({
  loadLearningNames: (...args: unknown[]) => loadLearningNames(...args),
  learningLabel: (names: Record<string, string>, id: string, unknown: string) => names[id] || unknown,
}));

vi.mock("@/lib/parent-workspace", () => ({
  fetchLinkedChildren: vi.fn().mockResolvedValue([{ parentUserId: "parent", studentUserId: studentId }]),
  fetchChildProgress: vi.fn().mockResolvedValue({
    studentUserId: studentId,
    organisationId,
    mastery: [{ microSkillId: skillId, masteryLevel: "Developing" }],
    feedback: [],
    assessments: [],
    activeInterventions: [],
  }),
}));

describe("parent home", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "33333333-3333-3333-3333-333333333333",
      email: "parent@school.local",
      name: "Demo Parent",
      roles: ["Parent"],
    });
    listParentChildren.mockReset().mockResolvedValue([{ id: studentId, name: "Demo Student" }]);
    loadLearningNames.mockReset().mockResolvedValue({ [skillId]: "Read an equation" });
  });

  afterEach(cleanup);

  it("shows the child and skill names and never a GUID", async () => {
    const { container } = render(
      <I18nProvider>
        <ParentWorkspacePage />
      </I18nProvider>
    );

    expect(await screen.findByRole("button", { name: "Child Demo Student" })).toBeInTheDocument();
    const skill = await screen.findByText("Read an equation");
    expect(skill.tagName).toBe("BDI");
    expect(skill.closest("li")).toHaveTextContent("Skill");
    expect(skill.closest("li")).toHaveTextContent("Developing");
    expect(skill).not.toHaveTextContent("Skill");
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });

  it("translates the mastery level instead of showing the raw code", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    render(
      <I18nProvider>
        <ParentWorkspacePage />
      </I18nProvider>
    );

    const skill = await screen.findByText("Read an equation");
    expect(skill.tagName).toBe("BDI");
    expect(skill).not.toHaveTextContent("المهارة");
    expect(skill.closest("li")).toHaveTextContent("نامٍ");
    expect(screen.queryByText(/Developing/)).not.toBeInTheDocument();
  });

  it("keeps the child name outside the Arabic label", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    render(
      <I18nProvider>
        <ParentWorkspacePage />
      </I18nProvider>
    );

    const name = await screen.findByText("Demo Student");
    expect(name.tagName).toBe("BDI");
    expect(await screen.findByRole("button", { name: "الطفل Demo Student" })).toBeInTheDocument();
    expect(name.parentElement).not.toHaveTextContent("الطفل");
  });

  it("keeps the welcome name and intervention skill outside the Arabic sentence", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    vi.mocked(fetchChildProgress).mockResolvedValue({
      studentUserId: studentId,
      organisationId,
      mastery: [{ microSkillId: skillId, masteryLevel: "Developing" }],
      feedback: [],
      assessments: [],
      activeInterventions: [
        {
          id: "int-1",
          summary:
            "Evidence Unknown skill: micro-skill Isolate the variable marked 2/5. Expected mastery: Mastered. Demonstrated: Developing.",
          status: "Active",
          plannedStartAt: null,
          plannedEndAt: null,
        },
      ],
    });

    render(
      <I18nProvider>
        <ParentWorkspacePage />
      </I18nProvider>
    );

    const welcome = await screen.findByText("Demo Parent");
    expect(welcome.tagName).toBe("BDI");
    expect(welcome.parentElement).not.toHaveTextContent("مرحباً");

    const skill = screen.getByText("Isolate the variable");
    expect(skill.tagName).toBe("BDI");
    expect(skill.parentElement).not.toHaveTextContent(/[\u0600-\u06FF]/);
    expect(screen.queryByText(/Mastered|Developing|Unknown skill/)).not.toBeInTheDocument();
    expect(screen.getByText("متقن")).toBeInTheDocument();
    expect(screen.getAllByText("نامٍ").length).toBeGreaterThan(0);
  });

  it("uses the marked micro-skill when the stored diagnostic says Evidence Unknown skill", async () => {
    vi.mocked(fetchChildProgress).mockResolvedValue({
      studentUserId: studentId,
      organisationId,
      mastery: [],
      feedback: [],
      assessments: [],
      activeInterventions: [
        {
          id: "int-stored",
          summary:
            "Address medium severity / medium urgency gap in micro-skill Isolate the variable.\n\n" +
            "Expected mastery: Mastered. Demonstrated: Developing (mark 2/5). " +
            "Micro-skill Isolate the variable has a medium-severity gap. " +
            "Based on diagnostic: Evidence Unknown skill: micro-skill Isolate the variable marked 2/5. " +
            'Teacher feedback: "You can substitute a number.". Classification: Developing.\n\n' +
            "Suggested actions: Provide scaffolded practice and monitor progress through the next assessment cycle.",
          status: "Active",
          plannedStartAt: null,
          plannedEndAt: null,
        },
      ],
    });

    render(
      <I18nProvider>
        <ParentWorkspacePage />
      </I18nProvider>
    );

    const skill = await screen.findByText("Isolate the variable");
    expect(skill.tagName).toBe("BDI");
    expect(screen.queryByText(/Unknown skill/)).not.toBeInTheDocument();
  });
});
