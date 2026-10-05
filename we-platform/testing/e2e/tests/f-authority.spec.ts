import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

test.describe("f. Authority", () => {
  test("authority user loads policy dashboards", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("authority") });
    const page = await context.newPage();

    const failed: string[] = [];
    page.on("response", (response) => {
      if (response.url().includes("/api/") && response.status() >= 500) {
        failed.push(`${response.status()} ${response.url()}`);
      }
    });

    await page.goto("/authority");
    await expect(
      page.getByRole("heading", { name: /Education Authority Policy Dashboards/i })
    ).toBeVisible();

    // Dashboard sections render (even if empty of national facts)
    await expect(page.getByText(/Policy|Equity|Curriculum|Intervention/i).first()).toBeVisible();
    expect(failed, `API 5xx on authority page: ${failed.join(", ")}`).toEqual([]);

    await context.close();
  });
});
