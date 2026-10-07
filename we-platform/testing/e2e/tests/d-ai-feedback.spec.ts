import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { waitForUi } from "../helpers/poll";
import { ALGEBRA_SKILLS } from "../helpers/users";
import { uniqueTitle } from "../helpers/unique";

test.describe("d. AI feedback draft", () => {
  test("teacher drafts/edits/finalises AI feedback; student never sees draft before approve; admin sees audit", async ({
    browser,
  }) => {
    test.setTimeout(180_000);
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
    await expect(teacherPage.getByRole("button", { name: "Approve evidence" })).toBeEnabled();

    // Controlled number inputs sometimes drop Playwright fill after AI re-render; type + assert.
    const markInputs = teacherPage.getByLabel("Mark");
    const feedbackInputs = teacherPage.getByLabel("Feedback");
    const count = await markInputs.count();
    expect(count).toBeGreaterThanOrEqual(4);
    for (let i = 0; i < count; i++) {
      const mark = markInputs.nth(i);
      await mark.click();
      await mark.fill("4");
      if ((await mark.inputValue()) !== "4") {
        await mark.clear();
        await mark.pressSequentially("4");
      }
      await expect(mark).toHaveValue("4");

      const feedback = feedbackInputs.nth(i);
      const feedbackText = i === 0 ? editedFeedback : `Mark ${i} ${title}`;
      await feedback.fill(feedbackText);
      await expect(feedback).toHaveValue(feedbackText);
    }
    await expect(feedbackInputs.first()).toHaveValue(editedFeedback);

    // A later solid mark on every demo skill would replace the open gap the
    // student home still has to show. Leave Isolate the variable at that gap.
    const isolate = teacherPage
      .getByText("Isolate the variable", { exact: true })
      .locator("xpath=ancestor::div[contains(@class,'p-3')][1]");
    const isolateMark = isolate.getByRole("spinbutton");
    await isolateMark.click();
    await isolateMark.fill("2");
    if ((await isolateMark.inputValue()) !== "2") {
      await isolateMark.clear();
      await isolateMark.pressSequentially("2");
    }
    await expect(isolateMark).toHaveValue("2");
    await isolate.getByLabel("Feedback").fill(
      "You can substitute a number. The next step is getting the letter alone on one side."
    );

    // Draft exists only for the teacher until approve — student UI must not show it yet.
    await studentPage.goto("/student/feedback");
    await expect(studentPage.getByText(editedFeedback)).toHaveCount(0);

    await teacherPage.getByRole("button", { name: "Approve evidence" }).click();
    await expect(teacherPage.getByText(/Submission approved/i)).toBeVisible();
    await expect(teacherPage.getByText(editedFeedback)).toBeVisible();

    await waitForUi(studentPage, "/student/feedback", async (page) => {
      await expect(page.getByText(editedFeedback)).toBeVisible();
    }, { timeoutMs: 90_000 });

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
