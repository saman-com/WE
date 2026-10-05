import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

const FORBIDDEN = /\b(gap|failure|weakness|severity)\b/i;
const LEVELS = /Strong|Solid|Getting there|Not yet/;

const studentPages = ["/student", "/student/progress", "/student/feedback", "/student/assessments"];

test.describe("h. Student wording", () => {
  for (const path of studentPages) {
    test(`${path} avoids deficit language and uses mastery levels where shown`, async ({
      browser,
    }) => {
      const context = await browser.newContext({ storageState: storagePath("student") });
      const page = await context.newPage();
      await page.goto(path);
      await page.waitForLoadState("networkidle");

      const bodyText = await page.locator("body").innerText();
      expect(bodyText, `Forbidden wording on ${path}`).not.toMatch(FORBIDDEN);

      if (path === "/student/feedback") {
        await expect(page.getByText(LEVELS).first()).toBeVisible({ timeout: 30_000 });
      }

      if (path === "/student") {
        // Skills tab surfaces Strong / Solid / Getting there / Not yet after seed evidence
        await page.getByRole("button", { name: /Skills|المهارات/i }).click();
        await expect(page.getByText(LEVELS).first()).toBeVisible({ timeout: 30_000 });
      }

      await context.close();
    });
  }
});
