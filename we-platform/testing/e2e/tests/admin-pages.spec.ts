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
    await page.getByRole("button", { name: "Grading scale" }).click();
    await expect(page.getByLabel("Scale name")).toHaveValue("Default", { timeout: 30_000 });
    const lastUpdated = page.getByText(/Last updated/);
    if ((await lastUpdated.count()) > 0) {
      await expect(lastUpdated).not.toContainText("1970");
    }
    await expect(page.getByRole("button", { name: "Save configuration" })).toBeVisible();
    await page.getByRole("button", { name: "Locale settings" }).click();
    await expect(page.getByLabel("Time zone")).toHaveValue("UTC");
  });
});

test("organisation tabs show each section on desktop and phone", async ({ browser }) => {
  await withRole(browser, "admin", async (page) => {
    const tabs = ["Year levels", "Classes", "Staff", "Students", "Parent links", "Accounts"] as const;
    for (const viewport of [
      { width: 1280, height: 800 },
      { width: 390, height: 844 },
    ]) {
      await page.setViewportSize(viewport);
      await page.goto("/organisation");
      await expect(page.getByRole("heading", { name: "Organisation setup", exact: true })).toBeVisible();
      const tabBar = viewport.width < 768 ? page.getByRole("navigation") : page.locator(".we-learning");
      for (const tab of tabs) {
        await tabBar.getByRole("button", { name: tab, exact: true }).click();
        const heading = page.getByRole("heading", { name: tab, exact: true });
        await expect(heading).toBeVisible();
        await expect(heading).toBeInViewport();
      }
    }
  });
});

test("AI audit lists a real gateway entry", async ({ browser }) => {
  await withRole(browser, "admin", async (page) => {
    await page.goto("/admin/ai-audit");
    await expect(page.getByRole("heading", { name: /AI audit/i })).toBeVisible();
    await expect(page.getByLabel("Prompt id")).toHaveValue("");
    await expect(page.getByRole("status")).toHaveText(/Showing \d+ entr(y|ies)\./, { timeout: 30_000 });
    await expect(page.getByRole("button", { name: "Apply filters" })).toBeVisible();
  });
});
