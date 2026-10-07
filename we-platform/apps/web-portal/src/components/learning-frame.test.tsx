import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LearningFrame } from "@/components/learning-frame";

describe("learning frame tabs", () => {
  it("wraps the tab row so a narrow screen does not scroll sideways", () => {
    render(
      <I18nProvider>
        <LearningFrame
          eyebrow="School"
          title="Demo school"
          tabs={[
            { id: "grading", label: "Grading scale" },
            { id: "models", label: "Assessment models" },
            { id: "templates", label: "Reporting templates" },
            { id: "locale", label: "Locale settings" },
          ]}
          activeTab="grading"
          onTabChange={() => undefined}
        >
          <p>Body</p>
        </LearningFrame>
      </I18nProvider>
    );

    const nav = screen.getByRole("navigation");
    const row = nav.querySelector("div");
    expect(row?.className).toContain("grid-cols-3");
    expect(row?.className).toContain("w-full");
    expect(row?.className).not.toContain("overflow-x-auto");
    expect(row?.className).not.toContain("flex-nowrap");
    for (const label of ["Grading scale", "Assessment models", "Reporting templates", "Locale settings"]) {
      const tabs = screen.getAllByRole("button", { name: label });
      const phoneTab = tabs.find((tab) => tab.className.includes("whitespace-normal"));
      expect(phoneTab).toBeTruthy();
      expect(phoneTab?.className).not.toContain("truncate");
      expect(phoneTab?.className).not.toContain("overflow-hidden");
    }
  });
});
