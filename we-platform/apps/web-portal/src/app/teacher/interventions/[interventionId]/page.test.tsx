import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import InterventionDetailPage from "@/app/teacher/interventions/[interventionId]/page";
import { I18nProvider } from "@/i18n/I18nProvider";
import type { UserProfile } from "@/lib/auth";
import type { Intervention } from "@/lib/interventions";

const replace = vi.fn();
const fetchProfile = vi.fn<(token: string) => Promise<UserProfile>>();
const fetchIntervention = vi.fn<(token: string, id: string) => Promise<Intervention>>();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, push: vi.fn() }),
  useParams: () => ({ interventionId: "intervention-1" }),
}));

vi.mock("@/lib/auth", () => ({
  fetchProfile: (token: string) => fetchProfile(token),
}));

vi.mock("@/lib/interventions", () => ({
  fetchIntervention: (token: string, id: string) => fetchIntervention(token, id),
  patchIntervention: vi.fn(),
}));

const intervention: Intervention = {
  id: "intervention-1",
  organisationId: "org-1",
  studentUserId: "student-1",
  learningGapId: "gap-1",
  assignedTeacherUserId: "teacher-1",
  plannedActions: "Practice isolating the variable.",
  notes: "Keep the practice short.",
  outcome: null,
  status: "Planned",
  plannedStartAt: null,
  plannedEndAt: null,
  reviewAt: null,
  createdAt: "2026-10-01T00:00:00Z",
  updatedAt: "2026-10-02T00:00:00Z",
};

function renderPage() {
  return render(
    <I18nProvider>
      <InterventionDetailPage />
    </I18nProvider>
  );
}

describe("intervention notes follow who the API allows to edit", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("we_access_token", "token");
    replace.mockReset();
    fetchProfile.mockReset();
    fetchIntervention.mockReset().mockResolvedValue(intervention);
  });

  afterEach(cleanup);

  it("shows notes read-only for a school leader", async () => {
    fetchProfile.mockResolvedValue({
      id: "leader-1",
      email: "leader@school.local",
      name: "Demo School Leader",
      roles: ["SchoolLeader"],
    });

    renderPage();

    expect(await screen.findByText("Keep the practice short.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save notes" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Mark as/ })).not.toBeInTheDocument();
  });

  it("lets the assigned teacher save notes", async () => {
    fetchProfile.mockResolvedValue({
      id: "teacher-1",
      email: "teacher@school.local",
      name: "Demo Teacher",
      roles: ["Teacher"],
    });

    renderPage();

    expect(await screen.findByRole("button", { name: "Save notes" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Notes" })).toHaveValue("Keep the practice short.");
  });

  it("shows notes read-only for a teacher who is not assigned", async () => {
    fetchProfile.mockResolvedValue({
      id: "teacher-2",
      email: "other@school.local",
      name: "Other Teacher",
      roles: ["Teacher"],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText("Keep the practice short.")).toBeInTheDocument();
    });
    expect(screen.queryByRole("button", { name: "Save notes" })).not.toBeInTheDocument();
  });
});
