import { afterEach, describe, expect, it, vi } from "vitest";
import {
  fetchCurriculumEffectiveness,
  fetchEquityAnalysis,
  fetchInterventionImpact,
  fetchPolicyTrends,
} from "@/lib/policy-dashboards";

const PAGE_LOAD_SLA_MS = 2000;
const API_LATENCY_MS = 450;

function delayedJson(body: unknown, delayMs: number): Promise<Response> {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(
        new Response(JSON.stringify(body), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        })
      );
    }, delayMs);
  });
}

describe("Phase 8 page-load SLA", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("authority policy dashboard data load completes within 2s when APIs respond within SLA", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/trends")) {
          return delayedJson(
            {
              asOfDate: "2026-10-01",
              totalSchools: 3,
              totalStudents: 400,
              nationalAverageMasteryPercent: 75,
              regions: [],
            },
            API_LATENCY_MS
          );
        }
        if (url.includes("/equity")) {
          return delayedJson({ asOfDate: "2026-10-01", distributions: [] }, API_LATENCY_MS);
        }
        if (url.includes("/curriculum-effectiveness")) {
          return delayedJson({ asOfDate: "2026-10-01", regions: [] }, API_LATENCY_MS);
        }
        if (url.includes("/intervention-impact")) {
          return delayedJson({ asOfDate: "2026-10-01", regions: [] }, API_LATENCY_MS);
        }
        return Promise.resolve(new Response("not found", { status: 404 }));
      })
    );

    const token = "test-token";
    const started = performance.now();
    await Promise.all([
      fetchPolicyTrends(token),
      fetchEquityAnalysis(token),
      fetchCurriculumEffectiveness(token),
      fetchInterventionImpact(token),
    ]);
    const elapsed = performance.now() - started;

    expect(elapsed).toBeLessThan(PAGE_LOAD_SLA_MS);
  });
});
