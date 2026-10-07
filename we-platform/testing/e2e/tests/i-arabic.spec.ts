import { test, expect, type Page } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import type { UserKey } from "../helpers/users";

const rawCodes = ["submitted", "Mastered", "Developing", "Proficient", "Medium", "Active", "Planned"];

async function expectNoRawCodes(page: Page) {
  const found = await page.evaluate((codes) => {
    const exact = new Set(codes);
    const word = new RegExp(`\\b(${codes.join("|")})\\b`);
    const arabic = /[\u0600-\u06FF]/;
    const hits: string[] = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    let node = walker.nextNode();
    while (node) {
      const text = (node.textContent ?? "").replace(/\s+/g, " ").trim();
      const parent = node.parentElement;
      if (!text || parent?.closest("bdi")) {
        node = walker.nextNode();
        continue;
      }
      if (exact.has(text) || (arabic.test(text) && word.test(text))) {
        hits.push(text.slice(0, 180));
      }
      node = walker.nextNode();
    }
    return hits;
  }, rawCodes);
  expect(found).toEqual([]);
}

async function useArabic(page: Page) {
  await page.getByRole("combobox", { name: /Language|اللغة/i }).selectOption("ar");
  await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
}

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

  test("student, teacher, and parent pages do not show raw status codes", async ({ browser }) => {
    const student = await browser.newContext({ storageState: storagePath("student") });
    const studentPage = await student.newPage();
    await studentPage.goto("/student");
    await useArabic(studentPage);
    const className = studentPage.locator("p", { hasText: "Year 11 Mathematics" }).first();
    await expect(className).toBeVisible();
    await expect(className).not.toContainText(/[\u0600-\u06FF]/);
    await expect(className.locator("bdi")).toHaveText("Year 11 Mathematics");
    const status = studentPage.locator("p", { hasText: "ما زال يحتاج تمرينًا" });
    await expect(status).toContainText("في الطريق");
    await expect(status).not.toContainText("letter alone");
    const feedback = studentPage.locator("bdi", { hasText: "the letter alone on one side" });
    await expect(feedback).toBeVisible();
    await expect(feedback.locator("xpath=..")).not.toContainText(/[\u0600-\u06FF]/);
    await expectNoRawCodes(studentPage);

    await studentPage.goto("/student/feedback");
    await useArabic(studentPage);
    const feedbackLine = studentPage.locator("bdi", { hasText: "Read an equation" }).first();
    await expect(feedbackLine).toBeVisible();
    await expect(feedbackLine).not.toContainText("قوي");
    await expect(feedbackLine.locator("xpath=..")).not.toContainText("قوي");
    const level = studentPage.getByText("قوي").first();
    await expect(level).toBeVisible();
    await expect(level).not.toContainText("Read an equation");
    const sentence = studentPage.locator("bdi", { hasText: "the letter alone on one side" }).first();
    await expect(sentence).toBeVisible();
    await expect(sentence).not.toContainText("في الطريق");
    await expect(sentence.locator("xpath=..")).not.toContainText("في الطريق");

    await studentPage.goto("/student/assessments");
    await useArabic(studentPage);
    const assessmentTitle = studentPage.locator("bdi", { hasText: "Algebra sheet" }).first();
    await expect(assessmentTitle).toBeVisible();
    await expect(assessmentTitle.locator("xpath=..")).not.toContainText("الاستحقاق");
    await studentPage.getByRole("button", { name: "Algebra sheet" }).first().click();
    await expect(studentPage.getByText(/تم التسليم في/)).toBeVisible();
    const submitted = studentPage.locator("p", { hasText: "تم التسليم في" });
    await expect(submitted.locator("bdi")).not.toContainText("تم التسليم");
    await expectNoRawCodes(studentPage);

    await studentPage.goto("/students/22222222-2222-2222-2222-222222222222/profile");
    await useArabic(studentPage);
    await expect(studentPage.getByText(/\b(Mastered|Developing|Proficient|Published)\b/)).toHaveCount(0);
    await expect(studentPage.getByText("متقن").first()).toBeVisible();
    await expect(studentPage.getByText("نامٍ").first()).toBeVisible();
    await student.close();

    const teacher = await browser.newContext({ storageState: storagePath("teacher") });
    const teacherPage = await teacher.newPage();
    await teacherPage.goto("/teacher");
    await useArabic(teacherPage);
    const openClass = teacherPage.getByRole("link", { name: "فتح" });
    await expect(openClass).not.toContainText("Year 11 Mathematics");
    const classTitle = teacherPage.locator("bdi", { hasText: "Year 11 Mathematics" }).first();
    await expect(classTitle).toBeVisible();
    await expect(classTitle.locator("xpath=ancestor::a")).toHaveCount(0);
    await expectNoRawCodes(teacherPage);
    await openClass.click();
    await expect(teacherPage).toHaveURL(/\/teacher\/classes\//);
    await expect(teacherPage.getByText(/\b(Mastered|Developing|Proficient|Published)\b/)).toHaveCount(0);
    await expect(teacherPage.getByText("متقن").first()).toBeVisible();
    await expect(teacherPage.getByText("ماهر").first()).toBeVisible();
    await expect(teacherPage.getByText("منشور").first()).toBeVisible();
    await expectNoRawCodes(teacherPage);
    await teacherPage.goto("/teacher/interventions");
    await expectNoRawCodes(teacherPage);
    await teacherPage.goto("/teacher/messages");
    await useArabic(teacherPage);
    const inboxStudent = teacherPage.locator("bdi", { hasText: "Demo Student" }).first();
    await expect(inboxStudent).toBeVisible();
    await expect(inboxStudent.locator("xpath=..")).not.toContainText("الطالب");
    const inboxParent = teacherPage.locator("bdi", { hasText: "Demo Parent" }).first();
    await expect(inboxParent).toBeVisible();
    await expect(inboxParent.locator("xpath=..")).not.toContainText("ولي الأمر");
    await teacher.close();

    const parent = await browser.newContext({ storageState: storagePath("parent") });
    const parentPage = await parent.newPage();
    await parentPage.goto("/parent");
    await useArabic(parentPage);
    await expectNoRawCodes(parentPage);
    const welcomeName = parentPage.locator("bdi", { hasText: "Demo Parent" }).first();
    await expect(welcomeName).toBeVisible();
    await expect(welcomeName.locator("xpath=..")).not.toContainText("مرحباً");
    const interventionSkill = parentPage.locator("bdi", { hasText: "Isolate the variable" }).first();
    await expect(interventionSkill).toBeVisible();
    await expect(interventionSkill.locator("xpath=..")).not.toContainText(/[\u0600-\u06FF]/);
    await expect(parentPage.getByText(/\b(Mastered|Developing)\b/)).toHaveCount(0);
    await expect(parentPage.getByText("متقن").first()).toBeVisible();

    await parentPage.goto("/dashboard");
    await useArabic(parentPage);
    const dashboardName = parentPage.locator("bdi", { hasText: "Demo Parent" }).first();
    await expect(dashboardName).toBeVisible();
    await expect(dashboardName.locator("xpath=..")).not.toContainText("الاسم");
    await expect(parentPage.getByText("الاسم:")).toBeVisible();

    await parentPage.goto("/parent/messages");
    const childName = parentPage.locator("bdi", { hasText: "Demo Student" }).first();
    await expect(childName).toBeVisible();
    await expect(childName.locator("xpath=..")).not.toContainText("الطفل");
    const teacherName = parentPage.locator("bdi", { hasText: "Demo Teacher" }).first();
    await expect(teacherName).toBeVisible();
    await expect(teacherName.locator("xpath=..")).not.toContainText("المعلم");
    await expectNoRawCodes(parentPage);
    await parent.close();
  });
});
