import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { waitForUi } from "../helpers/poll";
import { CLASS_A, ORG_A } from "../helpers/users";
import { uniqueTitle } from "../helpers/unique";

test.describe("b. Interventions", () => {
  test("teacher creates intervention from gap; parent sees it", async ({ browser }) => {
    const note = uniqueTitle("E2E intervention note");

    const teacher = await browser.newContext({ storageState: storagePath("teacher") });
    const teacherPage = await teacher.newPage();

    await teacherPage.goto(
      `/teacher/classes/${CLASS_A}?organisationId=${ORG_A}`
    );
    await expect(teacherPage.getByText(/Year 11 Mathematics|Create intervention|gaps/i).first()).toBeVisible({
      timeout: 60_000,
    });

    const createLink = teacherPage.getByRole("link", { name: "Create intervention" }).first();
    await expect(createLink).toBeVisible({ timeout: 60_000 });
    await createLink.click();

    await expect(teacherPage.getByRole("heading", { name: "Create intervention" })).toBeVisible();
    await teacherPage.getByLabel("Notes (optional)").fill(note);
    await teacherPage.getByRole("button", { name: "Create intervention" }).click();

    await expect(teacherPage).toHaveURL(/\/teacher\/interventions\//);
    await expect(teacherPage.getByText(/Status:/i)).toBeVisible();
    await teacherPage.getByRole("button", { name: "Mark as Active" }).click();
    await expect(teacherPage.getByText(/Active/i).first()).toBeVisible();

    const parent = await browser.newContext({ storageState: storagePath("parent") });
    const parentPage = await parent.newPage();

    await waitForUi(parentPage, "/parent", async (page) => {
      await expect(page.getByRole("heading", { name: /Active interventions/i })).toBeVisible();
      await expect(page.getByText(/Active|Planned/i).first()).toBeVisible();
    });

    await teacher.close();
    await parent.close();
  });
});
