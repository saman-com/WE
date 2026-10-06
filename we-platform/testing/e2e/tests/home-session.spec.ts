import { test, expect, type Page } from "@playwright/test";
import { withRole } from "../helpers/auth";
import { type UserKey, users } from "../helpers/users";

const roles = [
  "student",
  "teacher",
  "parent",
  "leader",
  "admin",
  "authority",
  "federation",
] as const satisfies readonly UserKey[];

const roleHomes: Record<(typeof roles)[number], RegExp> = {
  student: /\/student$/,
  teacher: /\/teacher$/,
  parent: /\/parent$/,
  leader: /\/leadership$/,
  admin: /\/organisation$/,
  authority: /\/authority$/,
  federation: /\/admin\/federation$/,
};

const roleData: Record<(typeof roles)[number], string | RegExp> = {
  student: "Algebra check",
  teacher: "Year 11 Mathematics",
  parent: "Algebra sheet",
  leader: "Year 11",
  admin: "Year 11",
  authority: /Total schools:\s*\d+/,
  federation: "North Federation School",
};

const homeOrBack = /\b(home|back)\b|الرئيسية|العودة/i;
const backToLogin = /back to login|العودة لتسجيل الدخول/i;

async function assertStillSignedIn(page: Page, token: string) {
  await expect
    .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
    .toBe(token);
  await expect(page).not.toHaveURL(/\/login$/);
  await expect(page.getByRole("link", { name: /^Sign in$|^تسجيل الدخول$/ })).toHaveCount(0);
}

async function clickHomeAndBackLinks(page: Page, token: string) {
  const seen = new Set<string>();
  for (let guard = 0; guard < 6; guard += 1) {
    const links = page.getByRole("link", { name: homeOrBack });
    const count = await links.count();
    let clicked = false;
    for (let index = 0; index < count; index += 1) {
      const link = links.nth(index);
      const label = ((await link.textContent()) ?? "").trim();
      if (backToLogin.test(label)) {
        continue;
      }
      const href = (await link.getAttribute("href")) ?? "";
      const key = `${label} ${href}`;
      if (seen.has(key)) {
        continue;
      }
      seen.add(key);
      await link.click();
      await assertStillSignedIn(page, token);
      clicked = true;
      break;
    }
    if (!clicked) {
      break;
    }
  }
}

for (const role of roles) {
  test(`${users[role].role} stays signed in through home and back links`, async ({ browser }) => {
    await withRole(browser, role, async (page) => {
      await page.goto("/");
      await expect(page).toHaveURL(roleHomes[role]);
      await expect(page.getByText(roleData[role]).first()).toBeVisible({ timeout: 30_000 });
      if (role === "student") {
        await expect(page.getByText("Algebra check")).toHaveCount(2);
      }
      if (role === "federation") {
        await expect(page.getByText("Shared curriculum: Enabled").first()).toBeVisible();
        await expect(page.getByText("No metrics available.")).toHaveCount(0);
        await page.getByRole("button", { name: "Add a school" }).click();
        await expect(page.getByLabel("School name")).toBeVisible();
        await page.getByRole("button", { name: "Overview" }).click();
        await expect(page.getByText("North Federation School").first()).toBeVisible();
      }
      const token = await page.evaluate(() => localStorage.getItem("we_access_token"));
      expect(token).toBeTruthy();
      await assertStillSignedIn(page, token!);

      await clickHomeAndBackLinks(page, token!);

      await page.goto("/dashboard");
      const backHome = page.getByRole("link", { name: /Back to home|العودة إلى الصفحة الرئيسية/ });
      await expect(backHome).toBeVisible();
      await backHome.click();
      await expect(page).toHaveURL(roleHomes[role]);
      await assertStillSignedIn(page, token!);
      await clickHomeAndBackLinks(page, token!);
    });
  });
}
