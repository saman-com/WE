import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import LoginPage from "@/app/login/page";
import { I18nProvider } from "@/i18n/I18nProvider";

const { push, fetchProfile } = vi.hoisted(() => ({
  push: vi.fn(),
  fetchProfile: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push, replace: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  login: vi.fn().mockResolvedValue({ accessToken: "token", expiresInSeconds: 900 }),
  fetchProfile: (...args: unknown[]) => fetchProfile(...args),
}));

describe("login landing", () => {
  afterEach(() => {
    cleanup();
    push.mockReset();
    fetchProfile.mockReset();
  });

  it("sends a parent to the parent home", async () => {
    fetchProfile.mockResolvedValue({
      id: "parent-1",
      email: "parent@school.local",
      name: "Demo Parent",
      roles: ["Parent"],
    });
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <LoginPage />
      </I18nProvider>
    );

    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(push).toHaveBeenCalledWith("/parent");
  });
});
