import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const workflowPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../../.github/workflows/ci.yml"
);

describe("frontend CI", () => {
  it("runs portal unit tests in the frontend job", () => {
    const workflow = readFileSync(workflowPath, "utf8");
    const frontend = workflow.split(/^  e2e:/m)[0]?.split(/^  frontend:/m)[1] ?? "";
    expect(frontend).toMatch(/^\s+- name: Test\n\s+run: pnpm test$/m);
  });
});
