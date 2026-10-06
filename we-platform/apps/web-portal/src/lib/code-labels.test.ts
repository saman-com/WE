import { describe, expect, it } from "vitest";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import { translate } from "@/i18n";
import { codeLabel } from "@/lib/code-labels";

const codes = ["submitted", "Mastered", "Developing", "Proficient", "Medium", "Active", "Planned"];

describe("code labels", () => {
  it("translates the status and level codes shown in Arabic", () => {
    const t = (key: string) => translate(ar, key, undefined, en);
    for (const code of codes) {
      const label = codeLabel(t, code);
      expect(label, code).not.toBe(code);
      expect(label).not.toMatch(/[A-Za-z]/);
    }
  });

  it("keeps an unknown code unchanged", () => {
    expect(codeLabel((key) => key, "Published")).toBe("Published");
  });
});
