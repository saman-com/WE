import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

test.describe("e. Leadership", () => {
  test("admin can open leadership and generate school summary PDF", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("admin") });
    const page = await context.newPage();

    await page.goto("/leadership");
    await expect(
      page.getByRole("heading", { name: /School Leadership Dashboard/i })
    ).toBeVisible();

    const downloadPromise = page.waitForEvent("download", { timeout: 60_000 }).catch(() => null);
    await page.getByRole("button", { name: "Generate report" }).click();
    await expect(page.getByText(/School summary report generated/i)).toBeVisible({
      timeout: 60_000,
    });
    await page.getByRole("button", { name: "Export PDF" }).click();
    const download = await downloadPromise;
    if (download) {
      expect(download.suggestedFilename().toLowerCase()).toMatch(/\.pdf$/);
    } else {
      await expect(page.getByText(/PDF exported/i)).toBeVisible();
    }

    await context.close();
  });

  test("school leader page is currently broken by assessment 403 aggregation", async ({
    browser,
  }) => {
    test.fixme(
      true,
      "SchoolLeader JWT forwarded to assessment-service returns 403 during leadership dashboard aggregation"
    );
    const context = await browser.newContext({ storageState: storagePath("leader") });
    const page = await context.newPage();
    await page.goto("/leadership");
    await expect(
      page.getByRole("heading", { name: /School Leadership Dashboard/i })
    ).toBeVisible();
    await context.close();
  });
});
