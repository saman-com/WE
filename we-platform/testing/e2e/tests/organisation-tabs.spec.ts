import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

const labels = {
  en: ["Year levels", "Classes", "Staff", "Students", "Parent links", "Accounts"],
  ar: ["المستويات الدراسية", "الصفوف", "الهيئة التعليمية", "الطلاب", "روابط أولياء الأمور", "الحسابات"],
} as const;

for (const locale of ["en", "ar"] as const) {
  test(`organisation tabs are fully visible and tappable at 390px (${locale})`, async ({
    browser,
  }) => {
    const context = await browser.newContext({
      storageState: storagePath("admin"),
      viewport: { width: 390, height: 844 },
    });
    const page = await context.newPage();
    await page.goto("/organisation");
    if (locale === "ar") {
      await page.getByRole("combobox", { name: /Language|اللغة/i }).selectOption("ar");
      await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
    }

    const scroll = await page.evaluate(() => ({
      scrollWidth: document.documentElement.scrollWidth,
      clientWidth: document.documentElement.clientWidth,
    }));
    expect(scroll.scrollWidth).toBeLessThanOrEqual(scroll.clientWidth);

    for (const label of labels[locale]) {
      const tab = page.getByRole("button", { name: label, exact: true });
      await expect(tab).toBeInViewport({ ratio: 1 });
      const box = await tab.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(391);
      expect(box!.y).toBeGreaterThanOrEqual(0);
      expect(box!.y + box!.height).toBeLessThanOrEqual(844);
      await tab.click();
      await expect(page.getByRole("heading", { name: label, exact: true })).toBeVisible();
    }

    await context.close();
  });
}
