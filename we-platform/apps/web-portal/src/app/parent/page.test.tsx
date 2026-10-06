import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import ParentWorkspacePage from "@/app/parent/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

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
    expect(await screen.findByText("Skill Read an equation — Developing")).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });
});
