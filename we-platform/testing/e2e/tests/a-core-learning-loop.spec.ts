import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { expectTextSoon, waitForUi } from "../helpers/poll";
import { ALGEBRA_SKILLS, STUDENT_A } from "../helpers/users";
import { uniqueTitle } from "../helpers/unique";

test.describe("a. Core learning loop", () => {
  test("teacher publishes assessment → student submits → teacher approves → progress updates", async ({
    browser,
  }) => {
    const title = uniqueTitle("E2E Algebra");
    const responseText = `E2E answers for ${title}`;

    const teacher = await browser.newContext({ storageState: storagePath("teacher") });
    const teacherPage = await teacher.newPage();

    await teacherPage.goto("/assessments");
    await expect(
      teacherPage.getByRole("heading", { name: "Assessments", exact: true })
    ).toBeVisible();

    for (const skill of ALGEBRA_SKILLS) {
      const checkbox = teacherPage.getByRole("checkbox", { name: skill });
      await expect(checkbox).toBeVisible({ timeout: 30_000 });
      await checkbox.check();
    }

    await teacherPage.getByLabel("Title").fill(title);
    await teacherPage.getByLabel("Instructions").fill("Complete the algebra questions.");
    await teacherPage.getByRole("button", { name: "Create draft" }).click();
    await expect(teacherPage.getByText("Assessment created in draft.")).toBeVisible();

    const assessmentCard = teacherPage.locator("li").filter({ hasText: title });
    await expect(assessmentCard).toBeVisible();
    await assessmentCard.getByRole("button", { name: "Publish" }).click();
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

    await studentPage.getByLabel(/Your responses|responses/i).fill(responseText);
    await studentPage.getByRole("button", { name: "Submit assessment" }).click();
    await expect(studentPage.getByText(/Assessment submitted/i)).toBeVisible();

    await teacherPage.goto("/assessments");
    await teacherPage
      .locator("li")
      .filter({ hasText: title })
      .getByRole("link", { name: "Review submissions" })
      .click();
    await expect(teacherPage.getByText(responseText)).toBeVisible();

    const markInputs = teacherPage.getByLabel("Mark");
    const feedbackInputs = teacherPage.getByLabel("Feedback");
    const markCount = await markInputs.count();
    expect(markCount).toBeGreaterThanOrEqual(4);
    for (let i = 0; i < markCount; i++) {
      await markInputs.nth(i).fill(String(5 - Math.min(i, 3)));
      await feedbackInputs.nth(i).fill(`E2E feedback ${i + 1} for ${title}`);
    }

    await teacherPage.getByRole("button", { name: "Approve evidence" }).click();
    await expect(
      teacherPage.getByText(
        "Submission approved. Evidence recorded on the student learning profile."
      )
    ).toBeVisible();
    await expect(teacherPage.getByText("Approved", { exact: true })).toBeVisible();
    await expect(teacherPage.getByRole("button", { name: "Approve evidence" })).toHaveCount(0);

    await expectTextSoon(studentPage, "/student/progress", title);
    await expectTextSoon(
      studentPage,
      "/student/feedback",
      /E2E feedback|Strong|Solid|Getting there|Not yet/
    );

    await waitForUi(teacherPage, `/students/${STUDENT_A}/profile`, async (page) => {
      await expect(page.getByText(title).first()).toBeVisible();
    });

    await teacherPage.goto("/assessments");
    await teacherPage
      .locator("li")
      .filter({ hasText: title })
      .getByRole("link", { name: "Review submissions" })
      .click();
    await expect(teacherPage.getByText("Approved", { exact: true })).toBeVisible();
    await expect(teacherPage.getByRole("button", { name: "Approve evidence" })).toHaveCount(0);

    await teacher.close();
    await student.close();
  });
});
