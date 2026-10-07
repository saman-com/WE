import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import TeacherMessagesPage from "@/app/teacher/messages/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";

const { studentId, parentId } = vi.hoisted(() => ({
  studentId: "22222222-2222-2222-2222-222222222222",
  parentId: "33333333-3333-3333-3333-333333333333",
}));

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
  listDirectoryUsers: vi.fn().mockResolvedValue([
    { id: studentId, name: "Demo Student", email: "student@school.local", roles: ["Student"] },
    { id: parentId, name: "Demo Parent", email: "parent@school.local", roles: ["Parent"] },
  ]),
  personName: (people: Array<{ id: string; name?: string }>, userId: string, unknown: string) =>
    people.find((person) => person.id === userId)?.name || unknown,
}));

vi.mock("@/lib/messaging", () => ({
  fetchMessageInbox: vi.fn().mockResolvedValue({
    threads: [
      {
        studentUserId: studentId,
        parentUserId: parentId,
        teacherUserId: "11111111-1111-1111-1111-111111111111",
        messageCount: 22,
        latestMessage: {
          id: "m-1",
          studentUserId: studentId,
          parentUserId: parentId,
          teacherUserId: "11111111-1111-1111-1111-111111111111",
          senderUserId: parentId,
          senderRole: "Parent",
          body: "How is algebra going?",
          createdAt: "2026-10-07T00:00:00Z",
        },
      },
    ],
  }),
  fetchConversation: vi.fn(),
  sendMessage: vi.fn(),
}));

describe("teacher inbox", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    fetchProfile.mockReset().mockResolvedValue({
      id: "11111111-1111-1111-1111-111111111111",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });
  });

  afterEach(cleanup);

  it("keeps the student and parent names outside the Arabic inbox line", async () => {
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
    expect(screen.getByText("22 رسالة")).toBeInTheDocument();
  });
});
