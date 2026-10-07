import { describe, expect, it } from "vitest";
import ar from "@/locales/ar.json";
import en from "@/locales/en.json";
import { translate } from "@/i18n";
import { codeLabel, localizeStoredText } from "@/lib/code-labels";

const codes = [
  "submitted",
  "Mastered",
  "Developing",
  "Proficient",
  "NotStarted",
  "Medium",
  "Active",
  "Planned",
  "Draft",
  "Published",
];

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
    expect(codeLabel((key) => key, "Archived")).toBe("Archived");
  });

  it("translates mastery and status words inside a stored sentence", () => {
    const t = (key: string) => translate(ar, key, undefined, en);
    const text = localizeStoredText(
      "Mastery level: Mastered. Demonstrated: Developing. Status: Published.",
      t
    );
    expect(text).toBe("Mastery level: متقن. Demonstrated: نامٍ. Status: منشور.");
    expect(text).not.toMatch(/\b(Mastered|Developing|Proficient|Published)\b/);
  });
});
