import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const scriptPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../scripts/seed-demo.sh"
);

const algebraSkills = [
  ["Read an equation", "00000000-0000-4000-8000-000000001001"],
  ["Substitute a value", "00000000-0000-4000-8000-000000001002"],
  ["Use inverse operations", "00000000-0000-4000-8000-000000001003"],
  ["Isolate the variable", "00000000-0000-4000-8000-000000001004"],
] as const;

describe("demo seed skill ids", () => {
  it("writes the algebra seed ids onto micro_skills even when the curriculum already exists", () => {
    const script = readFileSync(scriptPath, "utf8");
    const alignAt = script.search(/update micro_skills/);
    expect(alignAt).toBeGreaterThan(-1);
    const align = script.slice(alignAt);
    const createdOnly = script.slice(
      script.indexOf("if [[ -z $CURRICULUM_EXISTING ]]"),
      script.indexOf("\nfi\n")
    );
    expect(createdOnly).not.toMatch(/update micro_skills/);

    for (const [name, id] of algebraSkills) {
      expect(script).toContain(`${name}`);
      expect(script).toMatch(new RegExp(`${id}`));
      expect(align).toContain(name);
    }
    expect(align).toContain("SKILL_READ");
    expect(align).toContain("SKILL_SUBSTITUTE");
    expect(align).toContain("SKILL_INVERSE");
    expect(align).toContain("SKILL_ISOLATE");
  });
});
