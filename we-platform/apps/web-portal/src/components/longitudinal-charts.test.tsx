import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import {
  GapHistoryTimeline,
  InterventionOutcomesTimeline,
  MasteryTrendChart,
} from "@/components/longitudinal-charts";
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
});
