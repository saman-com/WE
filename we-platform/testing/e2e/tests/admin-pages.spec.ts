import { test, expect } from "@playwright/test";
import { withRole } from "../helpers/auth";

test("curriculum tree shows a real subject", async ({ browser }) => {
  await withRole(browser, "admin", async (page) => {
    await page.goto("/curriculum");
    await expect(page.getByRole("heading", { name: "Curriculum", exact: true })).toBeVisible();
    await expect(page.getByLabel("School")).toBeVisible();
    await page.getByLabel("Curriculum").selectOption({ label: "Year 11 Mathematics" });
    await expect(page.getByRole("button", { name: /Mathematics \(MATH\)/ })).toBeVisible({
      timeout: 30_000,
    });
    await expect(page.getByText("Algebra")).toBeVisible();
    await page.getByRole("button", { name: "Add", exact: true }).click();
    await expect(page.getByLabel("What to add")).toBeVisible();
  });
});
