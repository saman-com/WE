import type { GradingLevel } from "@/lib/regional-configuration";

export type GradingLevelProblem = "range" | "overlap";

export function validateGradingLevels(levels: GradingLevel[]): GradingLevelProblem | null {
  for (const level of levels) {
    if (
      !Number.isFinite(level.minScore) ||
      !Number.isFinite(level.maxScore) ||
      !(level.minScore < level.maxScore)
    ) {
      return "range";
    }
  }

  for (let left = 0; left < levels.length; left += 1) {
    for (let right = left + 1; right < levels.length; right += 1) {
      const a = levels[left];
      const b = levels[right];
      if (a.minScore <= b.maxScore && b.minScore <= a.maxScore) {
        return "overlap";
      }
    }
  }

  return null;
}

export function moveGradingLevel(
  levels: GradingLevel[],
  index: number,
  direction: -1 | 1
): GradingLevel[] {
  const next = index + direction;
  if (next < 0 || next >= levels.length) {
    return levels;
  }
  const copy = levels.slice();
  const [item] = copy.splice(index, 1);
  copy.splice(next, 0, item);
  return copy;
}
