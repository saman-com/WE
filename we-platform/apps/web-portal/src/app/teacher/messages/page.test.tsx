import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherMessagesPage from "@/app/teacher/messages/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listDirectoryUsers: vi.fn().mockResolvedValue([
      {
        id: "student-1",
        name: "Demo Student",
        email: "student@school.local",
        roles: ["Student"],
      },
      {
        id: "parent-1",
        name: "Demo Parent",
        email: "parent@school.local",
        roles: ["Parent"],
      },
    ]),
  };
});

vi.mock("@/lib/messaging", () => ({
  fetchMessageInbox: vi.fn().mockResolvedValue({
    threads: [
      {
        studentUserId: "student-1",
        parentUserId: "parent-1",
        teacherUserId: "teacher-1",
        messageCount: 1,
        latestMessage: {
          id: "message-1",
          studentUserId: "student-1",
          parentUserId: "parent-1",
          teacherUserId: "teacher-1",
          senderUserId: "parent-1",
          senderRole: "Parent",
          body: "Please look at this.",
          createdAt: "2026-10-06T09:00:00.000Z",
        },
      },
    ],
  }),
  fetchConversation: vi.fn(),
  sendMessage: vi.fn(),
}));

describe("teacher message inbox", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    } satisfies UserProfile);
  });

  afterEach(cleanup);

  it("labels the thread with the student and parent names", async () => {
    render(
      <I18nProvider>
        <TeacherMessagesPage />
      </I18nProvider>
    );

    const thread = await screen.findByRole("button", { name: /Demo Student/ });
    expect(thread).toHaveTextContent("Demo Parent");
    expect(thread).not.toHaveTextContent("student-1");
    expect(thread).not.toHaveTextContent("parent-1");
  });

  it("keeps the student and parent names outside the Arabic labels", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    render(
      <I18nProvider>
        <TeacherMessagesPage />
      </I18nProvider>
    );

    const student = await screen.findByText("Demo Student");
    const parent = screen.getByText("Demo Parent");
    expect(student.tagName).toBe("BDI");
    expect(parent.tagName).toBe("BDI");
    expect(student.parentElement).not.toHaveTextContent("الطالب");
    expect(parent.parentElement).not.toHaveTextContent("ولي الأمر");
    expect(screen.getByText("الطالب")).toBeInTheDocument();
    expect(screen.getByText("ولي الأمر")).toBeInTheDocument();
    expect(screen.getByText("رسالة واحدة")).toBeInTheDocument();
  });
});
