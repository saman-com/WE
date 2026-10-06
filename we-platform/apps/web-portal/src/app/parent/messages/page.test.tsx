import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import ParentMessagesPage from "@/app/parent/messages/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ApiError } from "@/lib/api-error";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const sendMessage = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
  };
});

vi.mock("@/lib/parent-workspace", () => ({
  fetchLinkedChildren: vi.fn().mockResolvedValue([
    { studentUserId: "22222222-2222-2222-2222-222222222222" },
  ]),
}));

vi.mock("@/lib/messaging", () => ({
  fetchMessageInbox: vi.fn().mockResolvedValue({ threads: [] }),
  fetchConversation: vi.fn(),
  sendMessage: (...args: unknown[]) => sendMessage(...args),
}));

describe("parent message send errors", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "parent-1",
      email: "parent@school.local",
      name: "Demo Parent",
      roles: ["Parent"],
    } satisfies UserProfile);
    sendMessage.mockReset().mockRejectedValue(new ApiError("messages.invalid_teacher", 400));
  });

  afterEach(cleanup);

  it("keeps the form and says the teacher is not valid", async () => {
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <ParentMessagesPage />
      </I18nProvider>
    );

    await screen.findByRole("button", { name: "Send message" });
    await user.type(screen.getByLabelText("Teacher user ID"), "not-a-teacher");
    await user.type(screen.getByLabelText("Message"), "Please look at this.");
    await user.click(screen.getByRole("button", { name: "Send message" }));

    expect(await screen.findByText("That teacher is not valid.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Send message" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Back to login" })).not.toBeInTheDocument();
    expect(localStorage.getItem("we_access_token")).toBe("token");
  });
});
