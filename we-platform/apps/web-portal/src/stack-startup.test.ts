import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const scriptPath = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../../testing/e2e/scripts/run-e2e.sh"
);

describe("stack startup retry", () => {
  it("dumps docker compose logs for exited containers before it retries", () => {
    const script = readFileSync(scriptPath, "utf8");
    const startup = script.slice(0, script.indexOf("echo \"==> wait for /health\""));
    const logsAt = startup.search(/docker compose logs\b/);
    const retryAt = startup.search(/retrying docker compose up once/);

    expect(startup).toMatch(/--status exited/);
    expect(logsAt).toBeGreaterThan(-1);
    expect(retryAt).toBeGreaterThan(logsAt);
  });
});
