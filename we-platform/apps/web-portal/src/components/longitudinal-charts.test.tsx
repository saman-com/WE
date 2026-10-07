import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import {
  GapHistoryTimeline,
  InterventionOutcomesTimeline,
  MasteryTrendChart,
} from "@/components/longitudinal-charts";
import { LOCALE_STORAGE_KEY } from "@/i18n";
import { I18nProvider } from "@/i18n/I18nProvider";

function renderWithI18n(ui: React.ReactElement) {
  return render(<I18nProvider>{ui}</I18nProvider>);
}

describe("Longitudinal charts empty states", () => {
  afterEach(cleanup);

  it("renders mastery empty copy when there are no points", () => {
    renderWithI18n(<MasteryTrendChart points={[]} />);

    expect(screen.getByText("No longitudinal mastery data yet.")).toBeInTheDocument();
  });

  it("renders gap history empty copy when there are no events", () => {
    renderWithI18n(<GapHistoryTimeline events={[]} />);

    expect(screen.getByText("No gap lifecycle events recorded yet.")).toBeInTheDocument();
  });

  it("renders intervention outcomes empty copy when there are no outcomes", () => {
    renderWithI18n(<InterventionOutcomesTimeline outcomes={[]} />);

    expect(
      screen.getByText("No intervention history in analytics store yet.")
    ).toBeInTheDocument();
  });

  it("shows the skill name for a gap and never a GUID", () => {
    const gapId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    const { container } = renderWithI18n(
      <>
        <GapHistoryTimeline
          events={[
            {
              learningGapId: gapId,
              eventType: "Opened",
              occurredAt: "2026-10-01T00:00:00Z",
            },
          ]}
          gapNames={{ [gapId]: "Read an equation" }}
        />
        <InterventionOutcomesTimeline
          outcomes={[
            {
              interventionId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
              learningGapId: gapId,
              status: "Active",
              createdAt: "2026-10-02T00:00:00Z",
            },
          ]}
          gapNames={{ [gapId]: "Read an equation" }}
        />
      </>
    );

    expect(screen.getAllByText(/Read an equation/).length).toBe(2);
    expect(screen.getByText("Gap opened")).toBeInTheDocument();
    expect(container.textContent ?? "").not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
  });

  it("translates opened and closed instead of leaving the English word in the Arabic line", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    const gapId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    renderWithI18n(
      <GapHistoryTimeline
        events={[
          {
            learningGapId: gapId,
            eventType: "Opened",
            occurredAt: "2026-10-01T00:00:00Z",
          },
          {
            learningGapId: gapId,
            eventType: "Closed",
            occurredAt: "2026-10-02T00:00:00Z",
          },
        ]}
        gapNames={{ [gapId]: "Read an equation" }}
      />
    );

    expect(await screen.findByText("الفجوة مفتوحة")).toBeInTheDocument();
    expect(screen.getByText("الفجوة مغلقة")).toBeInTheDocument();
    expect(screen.queryByText(/\bopened\b/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/\bclosed\b/i)).not.toBeInTheDocument();
    localStorage.removeItem(LOCALE_STORAGE_KEY);
  });
});
