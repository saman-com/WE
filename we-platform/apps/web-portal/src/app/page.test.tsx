import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import Home from "@/app/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

const student: UserProfile = {
  id: "student-1",
  email: "student@school.local",
  name: "Ava Student",
  roles: ["Student"],
};

function renderHome() {
  return render(
    <I18nProvider>
      <Home />
    </I18nProvider>
  );
}

describe("public landing", () => {
  beforeEach(() => {
    localStorage.clear();
    replace.mockReset();
    fetchProfile.mockReset();
  });

  afterEach(cleanup);

  it("shows Sign in when there is no session", async () => {
    renderHome();

    const signIn = await screen.findByRole("link", { name: "Sign in" });
    expect(signIn).toHaveAttribute("href", "/login");
    expect(replace).not.toHaveBeenCalled();
  });

  it("redirects a signed-in student to the student home instead of Sign in", async () => {
    localStorage.setItem("we_access_token", "token");
    fetchProfile.mockResolvedValue(student);
    renderHome();

    await waitFor(() => {
      expect(replace).toHaveBeenCalledWith("/student");
    });
    expect(screen.queryByRole("link", { name: "Sign in" })).not.toBeInTheDocument();
    expect(localStorage.getItem("we_access_token")).toBe("token");
  });
});
