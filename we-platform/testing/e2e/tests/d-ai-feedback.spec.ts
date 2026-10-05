import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { waitForUi } from "../helpers/poll";
import { ALGEBRA_SKILLS } from "../helpers/users";
import { uniqueTitle } from "../helpers/unique";

test.describe("d. AI feedback draft", () => {
  test("teacher drafts/edits/finalises AI feedback; student sees after approve; admin sees audit", async ({
    browser,
  }) => {
    const title = uniqueTitle("E2E AI Algebra");
    const editedFeedback = `Edited AI feedback ${title}`;

    const teacher = await browser.newContext({ storageState: storagePath("teacher") });
    const teacherPage = await teacher.newPage();

    await teacherPage.goto("/assessments");
    for (const skill of ALGEBRA_SKILLS) {
      await teacherPage.getByRole("checkbox", { name: skill }).check();
    }
    await teacherPage.getByLabel("Title").fill(title);
    await teacherPage.getByRole("button", { name: "Create draft" }).click();
    await expect(teacherPage.getByText("Assessment created in draft.")).toBeVisible();
    await teacherPage
      .locator("li")
      .filter({ hasText: title })
      .getByRole("button", { name: "Publish" })
      .click();
    await expect(teacherPage.getByText("Assessment published for students.")).toBeVisible();

    const student = await browser.newContext({ storageState: storagePath("student") });
    const studentPage = await student.newPage();

    await waitForUi(
      studentPage,
      "/student/assessments",
      async (page) => {
        await expect(page.getByRole("button", { name: title })).toBeVisible();
      },
      { timeoutMs: 90_000 }
    );
    await studentPage.getByRole("button", { name: title }).click();
    await studentPage.getByLabel(/Your responses|responses/i).fill(`Answers ${title}`);
    await studentPage.getByRole("button", { name: "Submit assessment" }).click();
    await expect(studentPage.getByText(/Assessment submitted/i)).toBeVisible();

    await studentPage.goto("/student/feedback");
    await expect(studentPage.getByText(editedFeedback)).toHaveCount(0);

    await teacherPage.goto("/assessments");
    await teacherPage
      .locator("li")
      .filter({ hasText: title })
      .getByRole("link", { name: "Review submissions" })
      .click();

    await teacherPage.getByRole("button", { name: "Draft with AI" }).first().click();
    await expect(teacherPage.getByText(/AI draft ready for your review/i)).toBeVisible({
      timeout: 60_000,
    });

    const firstFeedback = teacherPage.getByLabel("Feedback").first();
    await firstFeedback.fill(editedFeedback);

    const markInputs = teacherPage.getByLabel("Mark");
    const count = await markInputs.count();
    for (let i = 0; i < count; i++) {
      await markInputs.nth(i).fill("4");
      if (i > 0) {
        await teacherPage.getByLabel("Feedback").nth(i).fill(`Mark ${i} ${title}`);
      }
    }

    await teacherPage.getByRole("button", { name: "Approve evidence" }).click();
    await expect(teacherPage.getByText(/Submission approved/i)).toBeVisible();

    await waitForUi(studentPage, "/student/feedback", async (page) => {
      await expect(page.getByText(editedFeedback)).toBeVisible();
    }, { timeoutMs: 120_000 });

    const admin = await browser.newContext({ storageState: storagePath("admin") });
    const adminPage = await admin.newPage();
    await adminPage.goto("/admin/ai-audit");
    await expect(adminPage.getByRole("heading", { name: /AI audit/i })).toBeVisible();
    await expect(adminPage.locator("table tbody tr").first()).toBeVisible({ timeout: 30_000 });

    await teacher.close();
    await student.close();
    await admin.close();
  });
});
