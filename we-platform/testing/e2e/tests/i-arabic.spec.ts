import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

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
});
