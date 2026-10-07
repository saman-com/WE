import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import StudentFeedbackPage from "@/app/student/feedback/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/evidence", () => ({
  listStudentFeedback: vi.fn().mockResolvedValue({
    items: [
      {
        id: "sheet",
        assessmentId: "sheet-1",
        title: "Algebra sheet",
        approvedAt: "2026-10-05T00:00:00Z",
        microSkillMarks: [
          { microSkillId: "read", mark: 5, feedback: "Read an equation" },
          { microSkillId: "substitute", mark: 4, feedback: "Substitute a value" },
          {
            microSkillId: "isolate",
            mark: 2,
            feedback: "You can substitute a number. The next step is getting the letter alone on one side.",
          },
        ],
      },
    ],
    hasMore: false,
    nextCursor: null,
  }),
}));

describe("student feedback", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    fetchProfile.mockReset().mockResolvedValue({
      id: "22222222-2222-2222-2222-222222222222",
      email: "student@school.local",
      name: "Demo Student",
      roles: ["Student"],
    });
  });

  afterEach(cleanup);

  it("keeps the Arabic level outside the feedback isolate", async () => {
    render(
      <I18nProvider>
        <StudentFeedbackPage />
      </I18nProvider>
    );

    const feedback = await screen.findByText("Read an equation");
    expect(feedback.closest("bdi")).not.toHaveTextContent("قوي");
    expect(feedback.closest("p")).not.toHaveTextContent("قوي");
    expect(screen.getByText("قوي").closest("p")).not.toHaveTextContent("Read an equation");

    const sentence = screen.getByText(/letter alone on one side/);
    expect(sentence.closest("bdi")).not.toHaveTextContent("في الطريق");
    expect(sentence.closest("p")).not.toHaveTextContent("في الطريق");
    expect(screen.getByText("في الطريق").closest("p")).not.toHaveTextContent("letter alone");
  });
});
