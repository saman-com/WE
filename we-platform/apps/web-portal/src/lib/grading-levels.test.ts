import { describe, expect, it } from "vitest";
import { moveGradingLevel, validateGradingLevels } from "@/lib/grading-levels";

const pass = { label: "Pass", minScore: 50, maxScore: 100 };
const fail = { label: "Fail", minScore: 0, maxScore: 49.99 };

describe("grading levels", () => {
  it("accepts adjacent ranges and rejects overlaps or a minimum that is not below the maximum", () => {
    expect(validateGradingLevels([pass, fail])).toBeNull();
    expect(validateGradingLevels([{ label: "A", minScore: 90, maxScore: 90 }])).toBe("range");
    expect(validateGradingLevels([{ label: "A", minScore: 100, maxScore: 80 }])).toBe("range");
    expect(
      validateGradingLevels([
        { label: "A", minScore: 80, maxScore: 100 },
        { label: "B", minScore: 70, maxScore: 80 },
      ])
    ).toBe("overlap");
  });

  it("reorders a level up or down", () => {
    expect(moveGradingLevel([pass, fail], 1, -1).map((level) => level.label)).toEqual([
      "Fail",
      "Pass",
    ]);
    expect(moveGradingLevel([pass, fail], 0, -1)).toEqual([pass, fail]);
  });
});