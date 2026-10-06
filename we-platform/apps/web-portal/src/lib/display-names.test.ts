import { describe, expect, it } from "vitest";
import { replaceVisibleIds } from "@/lib/display-names";

describe("replaceVisibleIds", () => {
  it("names a known skill and drops an evidence id instead of calling it an unknown skill", () => {
    const evidenceId = "00000000-0000-4000-8000-000000000999";
    const skillId = "00000000-0000-4000-8000-000000001001";
    const text = `Evidence ${evidenceId}: micro-skill marked 5/5. Classification: Mastered. ${skillId}.`;

    const result = replaceVisibleIds(
      text,
      [],
      { [skillId]: "Read an equation" },
      "Unknown person",
      "Unknown skill"
    );

    expect(result).toContain("Read an equation");
    expect(result).not.toContain(evidenceId);
    expect(result).not.toContain(skillId);
    expect(result).not.toContain("Unknown skill");
    expect(result).not.toMatch(/Evidence/i);
  });
});
