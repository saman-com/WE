import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherInterventionsPage from "@/app/teacher/interventions/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { Intervention } from "@/lib/interventions";
import type { Organisation, SchoolClass } from "@/lib/organisation";

const studentId = "22222222-2222-2222-2222-222222222222";
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
const loadPeople = vi.fn();
const loadLearningNames = vi.fn();
const loadGapLabels = vi.fn();
const listOrganisations = vi.fn<(token: string) => Promise<Organisation[]>>();
const listClasses = vi.fn<(token: string, organisationId: string) => Promise<SchoolClass[]>>();
const fetchStudentInterventions = vi.fn<
  (token: string, studentUserId: string) => Promise<{ studentUserId: string; interventions: Intervention[] }>
>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
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

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
  listClasses: (token: string, organisationId: string) => listClasses(token, organisationId),
}));

vi.mock("@/lib/interventions", () => ({
  fetchStudentInterventions: (token: string, studentUserId: string) =>
    fetchStudentInterventions(token, studentUserId),
}));

const teacher: UserProfile = {
  id: "teacher-chen",
  email: "chen@school.local",
  name: "Ms Chen",
  roles: ["Teacher"],
};

function intervention(
  partial: Partial<Intervention> & Pick<Intervention, "id" | "status" | "plannedActions">
): Intervention {
  return {
    organisationId: "org-1",
    studentUserId: "student-1",
    learningGapId: "gap-1",
    assignedTeacherUserId: teacher.id,
    notes: "",
    outcome: null,
    plannedStartAt: null,
    plannedEndAt: null,
    reviewAt: null,
    createdAt: "2026-10-01T00:00:00Z",
    updatedAt: "2026-10-02T00:00:00Z",
    ...partial,
  };
}

function renderPage() {
  return render(
    <I18nProvider>
      <TeacherInterventionsPage />
    </I18nProvider>
  );
}

describe("Teacher interventions list (UX-001 §10)", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(teacher);
    loadPeople.mockReset().mockResolvedValue([]);
    loadLearningNames.mockReset().mockResolvedValue({});
    loadGapLabels.mockReset().mockResolvedValue({});
    listOrganisations.mockReset().mockResolvedValue([
      { id: "org-1", name: "Harbour High", code: "HH" },
    ]);
    listClasses.mockReset().mockResolvedValue([
      {
        id: "c-maths",
        name: "Year 11 Mathematics",
        code: "11MAT",
        organisationId: "org-1",
        yearLevelId: "year-11",
        teacherUserIds: [teacher.id],
        studentUserIds: ["student-1"],
      },
    ]);
    fetchStudentInterventions.mockReset();
  });

  afterEach(cleanup);

  it("shows the empty sentence when no interventions exist", async () => {
    fetchStudentInterventions.mockResolvedValue({
      studentUserId: "student-1",
      interventions: [],
    });

    renderPage();

    expect(
      await screen.findByText(
        "No interventions yet. Create one from a student learning profile when a learning gap is identified."
      )
    ).toBeInTheDocument();
  });

  it("filters the list by status and shows empty copy when the filter matches nothing", async () => {
    const user = userEvent.setup();
    fetchStudentInterventions.mockResolvedValue({
      studentUserId: "student-1",
      interventions: [
        intervention({
          id: "i-active",
          status: "Active",
          plannedActions: "Guided algebra practice",
          updatedAt: "2026-10-03T00:00:00Z",
        }),
        intervention({
          id: "i-planned",
          status: "Planned",
          plannedActions: "Scaffolded substitution drills",
          updatedAt: "2026-10-02T00:00:00Z",
        }),
      ],
    });

    renderPage();

    expect(await screen.findByText("Guided algebra practice")).toBeInTheDocument();
    expect(screen.getByText("Scaffolded substitution drills")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Active" }));
    expect(screen.getByText("Guided algebra practice")).toBeInTheDocument();
    expect(screen.queryByText("Scaffolded substitution drills")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Closed" }));
    expect(
      screen.getByText(
        "No interventions yet. Create one from a student learning profile when a learning gap is identified."
      )
    ).toBeInTheDocument();
    expect(screen.queryByText("Guided algebra practice")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "All" }));
    expect(screen.getByText("Guided algebra practice")).toBeInTheDocument();
    expect(screen.getByText("Scaffolded substitution drills")).toBeInTheDocument();
  });

  it("shows the student and skill names and never a GUID", async () => {
    loadPeople.mockResolvedValue([
      { id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] },
    ]);
    loadGapLabels.mockResolvedValue({ [gapId]: "Read an equation" });
    loadLearningNames.mockResolvedValue({ [gapId]: "Read an equation" });
    fetchStudentInterventions.mockResolvedValue({
      studentUserId: studentId,
      interventions: [
        intervention({
          id: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          studentUserId: studentId,
          learningGapId: gapId,
          status: "Active",
          plannedActions: `Practice for ${studentId} on ${gapId}.`,
        }),
      ],
    });
    listClasses.mockResolvedValue([
      {
        id: "c-maths",
        name: "Year 11 Mathematics",
        code: "11MAT",
        organisationId: "org-1",
        yearLevelId: "year-11",
        teacherUserIds: [teacher.id],
        studentUserIds: [studentId],
      },
    ]);

    const { container } = renderPage();

    expect(await screen.findByText("Demo Student")).toBeInTheDocument();
    expect(screen.getByText("Practice for Demo Student on Read an equation.")).toBeInTheDocument();
    expect(screen.getByText(/Gap: Read an equation/)).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });

  it("sends a system administrator to the organisation home", async () => {
    fetchProfile.mockResolvedValue({
      id: "admin-1",
      email: "admin@school.local",
      name: "Demo Admin",
      roles: ["SystemAdministrator"],
    });

    renderPage();

    await vi.waitFor(() => {
      expect(replace).toHaveBeenCalledWith("/organisation");
    });
    expect(listOrganisations).not.toHaveBeenCalled();
    expect(screen.queryByText("Guided algebra practice")).not.toBeInTheDocument();
  });
});
