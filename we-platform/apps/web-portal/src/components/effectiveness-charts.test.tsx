import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import {
  CurriculumEffectivenessTable,
  InterventionEffectivenessTable,
} from "@/components/effectiveness-charts";
import { I18nProvider } from "@/i18n/I18nProvider";

function renderWithI18n(ui: React.ReactElement) {
  return render(<I18nProvider>{ui}</I18nProvider>);
}

describe("Effectiveness charts empty states", () => {
  afterEach(cleanup);

  it("renders curriculum empty copy when there are no items", () => {
    renderWithI18n(<CurriculumEffectivenessTable items={[]} />);

    expect(
      screen.getByText("No curriculum mastery data in the analytics warehouse yet.")
    ).toBeInTheDocument();
  });

  it("renders intervention empty copy when there are no items", () => {
    renderWithI18n(<InterventionEffectivenessTable items={[]} />);

    expect(
      screen.getByText("No intervention effectiveness data in the analytics warehouse yet.")
    ).toBeInTheDocument();
  });
});
