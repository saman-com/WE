import { test, expect, type Page } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { CLASS_A, ORG_A, STUDENT_A, type UserKey } from "../helpers/users";

const guidPattern = /[0-9a-f]{8}-[0-9a-f]{4}-/i;

const pagesByRole: Partial<Record<UserKey, string[]>> = {
  teacher: [
    "/",
    "/dashboard",
    "/notifications",
    "/curriculum",
    "/assessments",
    "/teacher",
    `/teacher/classes/${CLASS_A}?organisationId=${ORG_A}`,
    "/teacher/messages",
    "/teacher/interventions",
    `/students/${STUDENT_A}/profile`,
  ],
  student: ["/student", "/student/assessments", "/student/progress", "/student/feedback"],
  parent: ["/parent", "/parent/messages"],
  admin: ["/organisation", "/admin/ai-audit", "/admin/regional-configuration"],
  authority: ["/authority"],
  leader: ["/leadership"],
  federation: ["/admin/federation"],
};

function bareHexTokens(text: string): string[] {
  return text
    .split(/\s+/)
    .map((token) => token.replace(/^[^0-9a-f]+|[^0-9a-f]+$/gi, ""))
    .filter((token) => /^[0-9a-f]{8}$/i.test(token));
}

async function assertNoVisibleIds(page: Page, path: string) {
  await page.goto(path);
  await page.waitForLoadState("networkidle");
  await expect(page).not.toHaveURL(/\/login$/);
  const text = await page.locator("body").innerText();
  expect(text, `GUID visible on ${path}`).not.toMatch(guidPattern);
  expect(bareHexTokens(text), `Bare id visible on ${path}`).toEqual([]);
}

for (const [role, paths] of Object.entries(pagesByRole) as Array<[UserKey, string[]]>) {
  test(`${role} pages do not show ids`, async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath(role) });
    const page = await context.newPage();
    for (const path of paths) {
      await assertNoVisibleIds(page, path);
    }
    await context.close();
  });
}
