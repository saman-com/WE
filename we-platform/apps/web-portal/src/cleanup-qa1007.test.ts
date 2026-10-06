import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const scriptPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../scripts/cleanup-qa1007.sh"
);

describe("qa1007 cleanup", () => {
  it("removes the sweep leftovers and leaves the deactivated accounts", () => {
    const script = readFileSync(scriptPath, "utf8");
    expect(script).toContain("QA Year 1007");
    expect(script).toContain("QA1007");
    expect(script).toContain("QA check 1007");
    expect(script).toContain("QA School 1007");
    expect(script).toContain("qa-policy");
    expect(script).not.toMatch(/(delete|update).*"AspNetUsers"/i);
  });
});
