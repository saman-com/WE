import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import AiAuditAdminPage from "@/app/admin/ai-audit/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const callerId = "11111111-1111-1111-1111-111111111111";
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const { logs, directory } = vi.hoisted(() => {
  const callerId = "11111111-1111-1111-1111-111111111111";
  return {
    directory: [
      {
        id: callerId,
        name: "Demo Teacher",
        email: "teacher@school.local",
        roles: ["Teacher"],
      },
    ],
    logs: [
      {
        id: "log-1",
        callerUserId: callerId,
        promptId: "assessment-feedback",
        promptVersion: "1",
        contextScope: "assessment",
        outcome: "success",
        providerName: "Mock",
        createdAt: "2026-10-06T00:00:00.000Z",
      },
    ],
  };
});

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", async () => {
  const actual = await vi.importActual<typeof import("@/lib/auth")>("@/lib/auth");
  return {
    ...actual,
    fetchProfile: (token: string) => fetchProfile(token),
    listDirectoryUsers: vi.fn().mockResolvedValue(directory),
  };
});

vi.mock("@/lib/ai-gateway", () => ({
  searchAiAuditLogs: vi.fn().mockResolvedValue(logs),
}));

describe("AI audit callers", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockReset().mockResolvedValue({
      id: "admin-1",
      email: "admin@school.local",
      name: "Demo Admin",
      roles: ["SystemAdministrator"],
    } satisfies UserProfile);
  });

  afterEach(cleanup);

  it("shows the caller name instead of the user id", async () => {
    render(
      <I18nProvider>
        <AiAuditAdminPage />
      </I18nProvider>
    );

    expect((await screen.findAllByText("Demo Teacher")).length).toBeGreaterThan(0);
    expect(screen.queryByText(callerId)).not.toBeInTheDocument();
  });
});
