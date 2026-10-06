import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherHomePage from "@/app/teacher/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";
import type { Organisation, SchoolClass } from "@/lib/organisation";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const listOrganisations = vi.fn<(token: string) => Promise<Organisation[]>>();
const listClasses = vi.fn<(token: string, organisationId: string) => Promise<SchoolClass[]>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: (token: string) => listOrganisations(token),
  listClasses: (token: string, organisationId: string) => listClasses(token, organisationId),
}));

const teacher: UserProfile = {
  id: "teacher-chen",
  email: "chen@school.local",
  name: "Ms Chen",
  roles: ["Teacher"],
};

function schoolClass(partial: Partial<SchoolClass> & Pick<SchoolClass, "id" | "name" | "code">): SchoolClass {
  return {
    organisationId: "org-1",
    yearLevelId: "year-11",
    teacherUserIds: [teacher.id],
    studentUserIds: [],
    ...partial,
  };
}

function renderHome() {
  return render(
    <I18nProvider>
      <TeacherHomePage />
    </I18nProvider>
  );
}

function expectMutedLinks() {
  expect(screen.getByRole("link", { name: "Assessments" })).toHaveAttribute("href", "/assessments");
  expect(screen.getByRole("link", { name: "Curriculum" })).toHaveAttribute("href", "/curriculum");
  expect(screen.getByRole("link", { name: "Interventions" })).toHaveAttribute(
    "href",
    "/teacher/interventions"
  );
  expect(screen.getByRole("link", { name: "Parent messages" })).toHaveAttribute(
    "href",
    "/teacher/messages"
  );
}

describe("Teacher home (UX-001 §12)", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(teacher);
    listOrganisations.mockReset().mockResolvedValue([
      { id: "org-1", name: "Harbour High", code: "HH" },
    ]);
    listClasses.mockReset();
  });

  afterEach(cleanup);

  it("focuses the first class and opens it with the one filled action", async () => {
    listClasses.mockResolvedValue([
      schoolClass({ id: "c-maths", name: "Year 11 Mathematics", code: "11MAT", studentUserIds: ["s1", "s2", "s3"] }),
      schoolClass({ id: "c-stats", name: "Year 12 Statistics", code: "12STA", studentUserIds: ["s4"] }),
    ]);

    renderHome();

    expect(await screen.findByRole("heading", { name: "The classes you teach." })).toBeInTheDocument();
    expect(screen.getByText("WE")).toBeInTheDocument();
    expect(screen.getByText("Ms Chen")).toBeInTheDocument();

    expect(screen.getAllByText("Harbour High")[0]).toBeInTheDocument();
    expect(screen.getByText("Year 11 Mathematics (11MAT)")).toBeInTheDocument();
    expect(screen.getByText("3 students enrolled")).toBeInTheDocument();
    expect(screen.getByText("1 student enrolled")).toBeInTheDocument();

    const primary = screen.getByRole("link", { name: "Open Year 11 Mathematics" });
    expect(primary).toHaveAttribute("href", "/teacher/classes/c-maths?organisationId=org-1");
    expectMutedLinks();
  });

  it("lists further classes as rows that each open that class", async () => {
    listClasses.mockResolvedValue([
      schoolClass({ id: "c-maths", name: "Year 11 Mathematics", code: "11MAT" }),
      schoolClass({ id: "c-stats", name: "Year 12 Statistics", code: "12STA", studentUserIds: ["s4", "s5"] }),
    ]);

    renderHome();

    const row = (await screen.findAllByRole("listitem"))[0];
    const rowLink = within(row).getByRole("link");
    expect(rowLink).toHaveAttribute("href", "/teacher/classes/c-stats?organisationId=org-1");
    expect(within(rowLink).getByText("Year 12 Statistics (12STA)")).toBeInTheDocument();
    expect(within(rowLink).getByText("Harbour High")).toBeInTheDocument();
    expect(within(rowLink).getByText("2 students enrolled")).toBeInTheDocument();
  });

  it("shows the empty sentence and keeps the muted links when no class is assigned", async () => {
    listClasses.mockResolvedValue([]);

    renderHome();

    expect(await screen.findByText("No classes assigned to you.")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /^Open / })).not.toBeInTheDocument();
    expectMutedLinks();
  });

  it("does not show a class the teacher is not assigned to", async () => {
    listClasses.mockResolvedValue([
      schoolClass({ id: "c-other", name: "Year 10 English", code: "10ENG", teacherUserIds: ["someone-else"] }),
      schoolClass({ id: "c-maths", name: "Year 11 Mathematics", code: "11MAT" }),
    ]);

    renderHome();

    expect(
      await screen.findByRole("link", { name: "Open Year 11 Mathematics" })
    ).toBeInTheDocument();
    expect(screen.queryByText(/Year 10 English/)).not.toBeInTheDocument();
  });

  it("renders the home from the Arabic catalogue", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    listClasses.mockResolvedValue([
      schoolClass({ id: "c-maths", name: "Year 11 Mathematics", code: "11MAT" }),
    ]);

    renderHome();

    expect(await screen.findByRole("heading", { name: "الصفوف التي تدرّسها." })).toBeInTheDocument();
    const openClass = screen.getByRole("link", { name: "فتح Year 11 Mathematics" });
    const className = within(openClass).getByText("Year 11 Mathematics");
    expect(className.tagName).toBe("BDI");
    expect(className).not.toHaveTextContent("فتح");
    expect(screen.getByRole("link", { name: "التدخلات" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "رسائل أولياء الأمور" })).toBeInTheDocument();
  });

  it("sends a student to the student home instead of the dashboard", async () => {
    fetchProfile.mockResolvedValue({
      id: "student-1",
      email: "student@school.local",
      name: "Demo Student",
      roles: ["Student"],
    });

    renderHome();

    await vi.waitFor(() => {
      expect(replace).toHaveBeenCalledWith("/student");
    });
    expect(listOrganisations).not.toHaveBeenCalled();
  });
});
