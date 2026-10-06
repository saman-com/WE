import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

describe("production build output", () => {
  it("writes next build beside the folder the dev server uses", () => {
    const root = path.resolve(process.cwd());
    const pkg = JSON.parse(readFileSync(path.join(root, "package.json"), "utf8")) as {
      scripts: { dev: string; build: string; start: string };
    };
    const config = readFileSync(path.join(root, "next.config.ts"), "utf8");

    expect(pkg.scripts.dev).not.toContain("NEXT_DIST_DIR");
    expect(pkg.scripts.build).toContain("NEXT_DIST_DIR=.next-build");
    expect(pkg.scripts.start).toContain("NEXT_DIST_DIR=.next-build");
    expect(config).toContain('process.env.NEXT_DIST_DIR ?? ".next"');
  });
});
