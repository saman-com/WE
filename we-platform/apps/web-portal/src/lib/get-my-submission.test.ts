import { afterEach, describe, expect, it, vi } from "vitest";
import { getMySubmission } from "@/lib/assessment";

describe("getMySubmission", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("treats a missing submission as empty without reading a body", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 204,
        json: async () => {
          throw new Error("no body");
        },
      })
    );

    await expect(getMySubmission("token", "assessment-1")).resolves.toBeNull();
  });
});
