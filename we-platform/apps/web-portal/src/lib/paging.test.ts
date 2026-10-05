import { afterEach, describe, expect, it, vi } from "vitest";
import { listAssessments } from "@/lib/assessment";
import { listEvidenceForAssessment, listStudentFeedback } from "@/lib/evidence";
import { fetchStudentProfile } from "@/lib/student-learning";
import { UnexpectedPageError } from "@/lib/paging";

function jsonResponse(body: unknown) {
  return {
    ok: true,
    status: 200,
    json: async () => body,
  } as Response;
}

describe("paged list loaders", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("rejects a malformed assessments page instead of exposing undefined items", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(jsonResponse([{ id: "stale-row" }]))
    );

    await expect(listAssessments("token", "org", "class")).rejects.toBeInstanceOf(
      UnexpectedPageError
    );
  });

  it("rejects malformed evidence and student-profile pages", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ hasMore: false }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(listEvidenceForAssessment("token", "assessment")).rejects.toBeInstanceOf(
      UnexpectedPageError
    );
    await expect(listStudentFeedback("token")).rejects.toBeInstanceOf(UnexpectedPageError);
    await expect(fetchStudentProfile("token", "student")).rejects.toBeInstanceOf(
      UnexpectedPageError
    );
  });

  it("returns items when the page has the expected shape", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse({
          items: [{ id: "assessment-1" }],
          hasMore: false,
          nextCursor: null,
        })
      )
    );

    const page = await listAssessments("token", "org", "class");
    expect(page.items).toHaveLength(1);
    expect(page.hasMore).toBe(false);
    expect(page.nextCursor).toBeNull();
  });
});
