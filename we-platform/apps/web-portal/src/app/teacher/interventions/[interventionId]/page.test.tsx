import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import InterventionDetailPage from "@/app/teacher/interventions/[interventionId]/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { Intervention } from "@/lib/interventions";

const studentId = "22222222-2222-2222-2222-222222222222";
const teacherId = "11111111-1111-1111-1111-111111111111";
const gapId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

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
const fetchIntervention = vi.fn<(token: string, id: string) => Promise<Intervention>>();
const loadPeople = vi.fn();
const loadLearningNames = vi.fn();
const loadGapLabels = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
  useParams: () => ({ interventionId: "intervention-1" }),
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

vi.mock("@/lib/interventions", () => ({
  fetchIntervention: (token: string, id: string) => fetchIntervention(token, id),
  patchIntervention: vi.fn(),
}));

const intervention: Intervention = {
  id: "intervention-1",
  organisationId: "org-1",
  studentUserId: studentId,
  learningGapId: gapId,
  assignedTeacherUserId: teacherId,
  plannedActions: "Practice isolating the variable.",
  notes: "Keep the practice short.",
  outcome: null,
  status: "Planned",
  plannedStartAt: null,
  plannedEndAt: null,
  reviewAt: null,
  createdAt: "2026-10-01T00:00:00Z",
  updatedAt: "2026-10-02T00:00:00Z",
};

function renderPage() {
  return render(
    <I18nProvider>
      <InterventionDetailPage />
    </I18nProvider>
  );
}

describe("intervention notes follow who the API allows to edit", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset();
    fetchIntervention.mockReset().mockResolvedValue(intervention);
    loadPeople.mockReset().mockResolvedValue([
      { id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] },
      { id: teacherId, name: "Demo Teacher", email: "teacher@school.local", roles: ["Teacher"] },
    ]);
    loadLearningNames.mockReset().mockResolvedValue({});
    loadGapLabels.mockReset().mockResolvedValue({ [gapId]: "Read an equation" });
  });

  afterEach(cleanup);

  it("shows notes read-only for a school leader", async () => {
    fetchProfile.mockResolvedValue({
      id: "leader-1",
      email: "leader@school.local",
      name: "Demo School Leader",
      roles: ["SchoolLeader"],
    });

    renderPage();

    expect(await screen.findByText("Keep the practice short.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save notes" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Mark as/ })).not.toBeInTheDocument();
  });

  it("lets the assigned teacher save notes", async () => {
    fetchProfile.mockResolvedValue({
      id: teacherId,
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });

    renderPage();

    expect(await screen.findByRole("button", { name: "Save notes" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Notes" })).toHaveValue("Keep the practice short.");
  });

  it("shows names and never a GUID", async () => {
    fetchProfile.mockResolvedValue({
      id: teacherId,
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });

    const { container } = renderPage();

    expect(await screen.findByText("Demo Student")).toBeInTheDocument();
    expect(screen.getByText("Demo Teacher")).toBeInTheDocument();
    expect(screen.getByText("Read an equation")).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });

  it("shows notes read-only for a teacher who is not assigned", async () => {
    fetchProfile.mockResolvedValue({
      id: "teacher-2",
      email: "other@school.local",
      name: "Other Teacher",
      roles: ["Teacher"],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText("Keep the practice short.")).toBeInTheDocument();
    });
    expect(screen.queryByRole("button", { name: "Save notes" })).not.toBeInTheDocument();
  });
});
