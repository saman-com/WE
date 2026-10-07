import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const scriptPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../scripts/cleanup-duplicate-feedback.sh"
);

describe("duplicate feedback notification cleanup", () => {
  it("keeps the oldest identical feedback row and leaves a different body alone", () => {
    const script = readFileSync(scriptPath, "utf8");
    expect(script).toContain("Teacher feedback available");
    expect(script).toMatch(/distinct on \(recipient_user_id, title, body\)/);
    expect(script).toMatch(/order by recipient_user_id, title, body, created_at/);
    expect(script).not.toMatch(
      /delete from notifications where title = 'Teacher feedback available'\s*;/
    );
  });
});
