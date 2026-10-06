import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import type { UserKey } from "../helpers/users";

test.describe("i. Arabic locale", () => {
  test("student, teacher, and parent pages use RTL and keep Arabic after reload", async ({
    browser,
  }) => {
    for (const role of ["student", "teacher", "parent"] as const) {
      const home = role === "student" ? "/student" : role === "teacher" ? "/teacher" : "/parent";
      const context = await browser.newContext({ storageState: storagePath(role) });
      const page = await context.newPage();

      await page.goto(home);
      await page.getByRole("combobox", { name: /Language|اللغة/i }).selectOption("ar");

      await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
      await expect(page.locator("html")).toHaveAttribute("lang", "ar");

      // i18n chrome (seed class names / teacher feedback text may remain English product data)
      if (role === "student") {
        await expect(page.getByRole("button", { name: "تسجيل الخروج" })).toBeVisible();
        await expect(page.getByRole("button", { name: "اليوم" })).toBeVisible();
        await expect(page.getByText("My learning")).toHaveCount(0);
      } else if (role === "teacher") {
        await expect(page.getByRole("button", { name: "تسجيل الخروج" })).toBeVisible();
        await expect(page.getByRole("heading", { name: "الصفوف التي تدرّسها." })).toBeVisible();
        await expect(page.getByText("The classes you teach.")).toHaveCount(0);
      } else {
        await expect(page.getByRole("heading", { name: "مساحة عمل ولي الأمر" })).toBeVisible();
        await expect(page.getByRole("link", { name: "لوحة التحكم" })).toBeVisible();
        await expect(page.getByText("Parent workspace")).toHaveCount(0);
      }

      await page.reload();
      await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
      await expect
        .poll(async () => page.evaluate(() => localStorage.getItem("we_locale")))
        .toBe("ar");

      await context.close();
    }
  });

  test("Arabic student home isolates English feedback from the Arabic status", async ({
    browser,
  }) => {
    const context = await browser.newContext({ storageState: storagePath("student") });
    const page = await context.newPage();
    await page.goto("/student");
    await page.getByRole("combobox", { name: /Language|اللغة/i }).selectOption("ar");

    const feedback = page.locator("bdi[dir='auto']", { hasText: "the letter alone on one side" });
    await expect(feedback).toBeVisible();
    await expect
      .poll(async () => feedback.evaluate((element) => getComputedStyle(element).unicodeBidi))
      .toBe("isolate");
    await expect(feedback).not.toContainText("في الطريق");
    await expect(page.getByText("في الطريق").first()).toBeVisible();

    await context.close();
  });

  test("admin and privileged pages use RTL", async ({ browser }) => {
    const routes: { role: UserKey; path: string }[] = [
      { role: "federation", path: "/admin/federation" },
      { role: "admin", path: "/admin/ai-audit" },
      { role: "admin", path: "/admin/regional-configuration" },
      { role: "admin", path: "/organisation" },
      { role: "authority", path: "/authority" },
      { role: "leader", path: "/leadership" },
    ];

    for (const { role, path } of routes) {
      const context = await browser.newContext({ storageState: storagePath(role) });
      const page = await context.newPage();

      await page.goto(path);
      await page.getByRole("combobox", { name: /Language|اللغة/i }).selectOption("ar");

      await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
      await expect(page.locator("html")).toHaveAttribute("lang", "ar");
      await expect(page).not.toHaveURL(/\/login$/);

      await context.close();
    }
  });
});
