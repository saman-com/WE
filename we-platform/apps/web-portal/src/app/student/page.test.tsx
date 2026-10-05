import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import StudentHomePage from "@/app/student/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { StudentWorkspace } from "@/lib/student-workspace";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const fetchStudentWorkspace = vi.fn<(token: string, studentUserId: string) => Promise<StudentWorkspace>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/student-workspace", () => ({
  fetchStudentWorkspace: (token: string, studentUserId: string) =>
    fetchStudentWorkspace(token, studentUserId),
}));

const student: UserProfile = {
  id: "student-ava",
  email: "ava@school.local",
  name: "Ava Student",
  roles: ["Student"],
};

const emptyWorkspace: StudentWorkspace = {
  studentUserId: student.id,
  assessments: [],
  feedback: [],
  timeline: [],
};

function renderHome() {
  return render(
    <I18nProvider>
      <StudentHomePage />
    </I18nProvider>
  );
}

describe("Student home empty states (UX-001 §8)", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset().mockResolvedValue(student);
    fetchStudentWorkspace.mockReset().mockResolvedValue(emptyWorkspace);
  });

  afterEach(cleanup);

  it("shows the quiet Today headline when nothing is waiting", async () => {
    renderHome();

    expect(await screen.findByRole("heading", { name: "Nothing is waiting." })).toBeInTheDocument();
  });

  it("shows empty copy on Skills, Next, Notes, and Growth tabs", async () => {
    const user = userEvent.setup();
    renderHome();

    expect(await screen.findByRole("heading", { name: "Nothing is waiting." })).toBeInTheDocument();

    // Desktop + mobile tab buttons share labels; click the first match for each.
    await user.click(screen.getAllByRole("button", { name: "Skills" })[0]);
    expect(
      await screen.findByText("Skills show up here after your teacher approves a piece of work.")
    ).toBeInTheDocument();

    await user.click(screen.getAllByRole("button", { name: "Next" })[0]);
    expect(
      await screen.findByText(
        "When a skill is still shaky, it will be the one practice on this screen."
      )
    ).toBeInTheDocument();

    await user.click(screen.getAllByRole("button", { name: "Notes" })[0]);
    expect(
      await screen.findByText("Notes show up after your teacher approves your work.")
    ).toBeInTheDocument();

    await user.click(screen.getAllByRole("button", { name: "Growth" })[0]);
    expect(
      await screen.findByText(
        "Growth shows up as approved work, compared with your earlier work."
      )
    ).toBeInTheDocument();
  });
});
