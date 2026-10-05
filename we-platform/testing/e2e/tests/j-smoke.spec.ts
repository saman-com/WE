import { test, expect, type Page } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { CLASS_A, ORG_A, STUDENT_A } from "../helpers/users";

async function assertPageLoads(page: Page, path: string) {
  const consoleErrors: string[] = [];
  const apiFailures: string[] = [];

  page.on("console", (msg) => {
    if (msg.type() === "error") {
      consoleErrors.push(msg.text());
    }
  });
  page.on("pageerror", (err) => {
    consoleErrors.push(String(err));
  });
  page.on("response", (response) => {
    if (response.url().includes("/api/") && response.status() >= 500) {
      apiFailures.push(`${response.status()} ${response.url()}`);
    }
  });

  await page.goto(path);
  await page.waitForLoadState("networkidle");
  await expect(page).not.toHaveURL(/\/login$/);
  await expect(page.locator("body")).toBeVisible();

  const serious = consoleErrors.filter(
    (line) => !/Download the React DevTools|favicon|hydration/i.test(line)
  );
  expect(serious, `Console errors on ${path}: ${serious.join(" | ")}`).toEqual([]);
  expect(apiFailures, `API failures on ${path}: ${apiFailures.join(" | ")}`).toEqual([]);
}

test.describe("j. Smoke pages", () => {
  test("teacher home and workspace routes", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("teacher") });
    const page = await context.newPage();
    await assertPageLoads(page, "/");
    await assertPageLoads(page, "/dashboard");
    await assertPageLoads(page, "/notifications");
    await assertPageLoads(page, "/curriculum");
    await assertPageLoads(page, "/assessments");
    await assertPageLoads(page, "/teacher");
    await assertPageLoads(page, `/teacher/classes/${CLASS_A}?organisationId=${ORG_A}`);
    await assertPageLoads(page, "/teacher/messages");
    await assertPageLoads(page, "/teacher/interventions");
    await assertPageLoads(page, `/students/${STUDENT_A}/profile`);
    await context.close();
  });

  test("student pages", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("student") });
    const page = await context.newPage();
    await assertPageLoads(page, "/student");
    await assertPageLoads(page, "/student/assessments");
    await assertPageLoads(page, "/student/progress");
    await assertPageLoads(page, "/student/feedback");
    await context.close();
  });

  test("parent pages", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("parent") });
    const page = await context.newPage();
    await assertPageLoads(page, "/parent");
    await assertPageLoads(page, "/parent/messages");
    await context.close();
  });

  test("admin pages", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("admin") });
    const page = await context.newPage();
    await assertPageLoads(page, "/organisation");
    await assertPageLoads(page, "/admin/ai-audit");
    await assertPageLoads(page, "/admin/regional-configuration");
    await context.close();
  });

  test("authority page", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("authority") });
    const page = await context.newPage();
    await assertPageLoads(page, "/authority");
    await context.close();
  });

  test("login page loads without auth", async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await page.goto("/login");
    await expect(page.getByRole("heading", { name: /Login|تسجيل/i })).toBeVisible();
    await context.close();
  });
});
