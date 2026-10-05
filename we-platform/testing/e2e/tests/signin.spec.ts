import { test, expect } from "@playwright/test";
import { loginViaUi, withRole } from "../helpers/auth";
import { users } from "../helpers/users";

test.describe("Sign-in session", () => {
  test("token survives page refresh", async ({ browser }) => {
    await withRole(browser, "teacher", async (page) => {
      await page.goto("/teacher");
      await expect(page).not.toHaveURL(/\/login/);
      await expect(page.getByRole("button", { name: /Sign out|تسجيل الخروج/i })).toBeVisible();

      const tokenBefore = await page.evaluate(() => localStorage.getItem("we_access_token"));
      expect(tokenBefore).toBeTruthy();

      await page.reload();

      await expect(page).not.toHaveURL(/\/login/);
      await expect(page.getByRole("button", { name: /Sign out|تسجيل الخروج/i })).toBeVisible();
      await expect
        .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
        .toBe(tokenBefore);
    });
  });

  test("logout clears session and blocks protected pages", async ({ browser }) => {
    await withRole(browser, "teacher", async (page) => {
      await page.goto("/teacher");
      await expect(page).not.toHaveURL(/\/login/);
      await expect
        .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
        .not.toBeNull();

      await page.getByRole("button", { name: /Sign out|تسجيل الخروج/i }).click();
      await expect(page).toHaveURL(/\/login/);
      await expect
        .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
        .toBeNull();

      await page.goto("/teacher");
      await expect(page).toHaveURL(/\/login/);
      await expect
        .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
        .toBeNull();
    });
  });

  test("fresh login stores token then logout clears it", async ({ page }) => {
    // Identity has no logout API; UI clears localStorage only.
    await loginViaUi(page, users.teacher.email);
    await expect(page).toHaveURL(/\/teacher/);
    await expect
      .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
      .not.toBeNull();

    await page.getByRole("button", { name: /Sign out|تسجيل الخروج/i }).click();
    await expect(page).toHaveURL(/\/login/);
    await expect
      .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
      .toBeNull();

    await page.goto("/dashboard");
    await expect(page).toHaveURL(/\/login/);
  });
});
