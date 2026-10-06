import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import CurriculumPage from "@/app/curriculum/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";

const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/organisation", () => ({
  listOrganisations: vi.fn().mockResolvedValue([
    { id: "org-1", name: "Demo School", code: "DEMO" },
  ]),
}));

vi.mock("@/lib/curriculum", () => ({
  listCurricula: vi.fn().mockResolvedValue([]),
  getCurriculumTree: vi.fn(),
  createCurriculum: vi.fn(),
  createSubject: vi.fn(),
  createUnit: vi.fn(),
  createTopic: vi.fn(),
  createLearningObjective: vi.fn(),
  createMicroSkill: vi.fn(),
  inheritCurriculum: vi.fn(),
  updateUnit: vi.fn(),
  updateLearningObjective: vi.fn(),
}));

describe("curriculum add form", () => {
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

  it("starts the subject fields empty", async () => {
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <CurriculumPage />
      </I18nProvider>
    );

    await user.click((await screen.findAllByRole("button", { name: "Add" }))[0]);
    expect(screen.getByLabelText("Subject")).toHaveValue("");
    expect(screen.getByLabelText("Subject code")).toHaveValue("");
  });
});
