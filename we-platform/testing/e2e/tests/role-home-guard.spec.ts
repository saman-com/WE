import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";

const roles = ["student", "teacher", "parent", "leader", "admin", "authority", "federation"] as const;

const homes: Record<(typeof roles)[number], RegExp> = {
  student: /\/student$/,
  teacher: /\/teacher$/,
  parent: /\/parent$/,
  leader: /\/leadership$/,
  admin: /\/organisation$/,
  authority: /\/authority$/,
  federation: /\/admin\/federation$/,
};

const studentPages = ["/student/assessments", "/student/feedback", "/student/progress"] as const;

const denied = /only system administrators|for students only|يمكن لمسؤولي النظام|هذه الصفحة للطلاب فقط/i;

for (const role of roles) {
  test(`${role} is sent to their own home from organisation and student-only pages`, async ({
    browser,
  }) => {
    const context = await browser.newContext({ storageState: storagePath(role) });
    const page = await context.newPage();

    await page.goto("/organisation");
    await expect(page).toHaveURL(role === "admin" ? /\/organisation$/ : homes[role]);
    await expect(page.getByText(denied)).toHaveCount(0);

    for (const path of studentPages) {
      await page.goto(path);
      if (role === "student") {
        await expect(page).toHaveURL(new RegExp(`${path.replaceAll("/", "\\/")}$`));
      } else {
        await expect(page).toHaveURL(homes[role]);
      }
      await expect(page.getByText(denied)).toHaveCount(0);
    }

    await context.close();
  });
}
