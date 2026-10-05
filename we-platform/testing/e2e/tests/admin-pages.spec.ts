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

test("regional configuration shows the saved calendar and one save action", async ({ browser }) => {
  await withRole(browser, "admin", async (page) => {
    await page.goto("/admin/regional-configuration");
    await expect(page.getByRole("heading", { name: "Regional configuration", exact: true })).toBeVisible();
    await expect(page.getByText(/Last updated/)).toHaveCount(0);
    await page.getByRole("button", { name: "Grading scale" }).click();
    await expect(page.getByLabel("Scale name")).toHaveValue("Default", { timeout: 30_000 });
    await expect(page.getByRole("button", { name: "Save configuration" })).toBeVisible();
    await page.getByRole("button", { name: "Locale settings" }).click();
    await expect(page.getByLabel("Time zone")).toHaveValue("UTC");
  });
});

test("AI audit lists a real gateway entry", async ({ browser }) => {
  await withRole(browser, "admin", async (page) => {
    await page.goto("/admin/ai-audit");
    await expect(page.getByRole("heading", { name: /AI audit/i })).toBeVisible();
    await expect(page.getByLabel("Prompt id")).toHaveValue("");
    await expect(page.getByRole("cell", { name: "assessment-feedback" })).toBeVisible({
      timeout: 30_000,
    });
    await expect(page.getByRole("status")).toContainText("Showing");
    await expect(page.getByRole("button", { name: "Apply filters" })).toBeVisible();
  });
});
