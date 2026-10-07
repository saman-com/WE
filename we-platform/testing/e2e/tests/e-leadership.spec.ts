import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

test.describe("e. Leadership", () => {
  test("admin is sent home from leadership", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("admin") });
    const page = await context.newPage();

    await page.goto("/leadership");
    await expect(page).toHaveURL(/\/organisation$/);
    await expect(
      page.getByRole("heading", { name: /School Leadership Dashboard/i })
    ).toHaveCount(0);

    await context.close();
  });

  test("school leader opens leadership dashboard and generates a report", async ({
    browser,
  }) => {
    const context = await browser.newContext({ storageState: storagePath("leader") });
    const page = await context.newPage();
    await page.goto("/leadership");
    await expect(
      page.getByRole("heading", { name: /School Leadership Dashboard/i })
    ).toBeVisible();
    await expect(page.getByText(/error|forbidden|failed to load/i)).toHaveCount(0);

    await page.getByRole("button", { name: "Generate report" }).click();
    await expect(page.getByText(/School summary report generated|report generated/i)).toBeVisible({
      timeout: 60_000,
    });

    await context.close();
  });
});
