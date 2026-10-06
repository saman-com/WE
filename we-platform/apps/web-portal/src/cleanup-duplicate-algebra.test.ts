import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const scriptPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../scripts/cleanup-duplicate-algebra.sh"
);

describe("duplicate algebra cleanup", () => {
  it("keeps the oldest sheet and check and removes repeated notifications", () => {
    const script = readFileSync(scriptPath, "utf8");
    expect(script).toMatch(/order by created_at limit 1/);
    expect(script).toContain("Algebra sheet");
    expect(script).toContain("Algebra check");
    expect(script).toContain("educational_evidence");
    expect(script).toContain("learning_gaps");
    expect(script).toContain("Teacher feedback available");
    expect(script).toContain("Assessment available: Algebra sheet");
    expect(script).toContain("Assessment available: Algebra check");
    expect(script).toMatch(/distinct on \(title, related_entity_id\)/);
    expect(script).not.toMatch(/delete from notifications where recipient_user_id = '\$STUDENT'\s*;/);
  });
});
