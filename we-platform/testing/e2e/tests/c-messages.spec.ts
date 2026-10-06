import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { uniqueTitle } from "../helpers/unique";
import { users } from "../helpers/users";

test.describe("c. Parent and teacher messages", () => {
  test("parent sends message; teacher replies; notification marked read", async ({
    browser,
  }) => {
    const body = uniqueTitle("E2E parent message");
    const reply = uniqueTitle("E2E teacher reply");

    const parentContext = await browser.newContext({ storageState: storagePath("parent") });
    const parentPage = await parentContext.newPage();
    await parentPage.goto("/parent/messages");
    await expect(parentPage.getByRole("button", { name: /send/i })).toBeVisible();

    await parentPage.getByLabel("Teacher").selectOption(users.teacher.userId);
    await parentPage.getByRole("textbox", { name: /^Message$/i }).fill(body);
    await parentPage.getByRole("button", { name: /Send message/i }).click();
    await expect(parentPage.getByText(body)).toBeVisible({ timeout: 30_000 });
    await parentContext.close();

    const teacherContext = await browser.newContext({ storageState: storagePath("teacher") });
    const teacherPage = await teacherContext.newPage();
    await teacherPage.goto("/teacher/messages");
    await expect(teacherPage.getByText(body)).toBeVisible({ timeout: 30_000 });
    await teacherPage.getByText(body).click();
    await teacherPage.locator("textarea").fill(reply);
    await teacherPage.getByRole("button", { name: /send reply|send/i }).click();
    await expect(teacherPage.getByText(reply)).toBeVisible({ timeout: 30_000 });
    await teacherContext.close();

    const notifyContext = await browser.newContext({ storageState: storagePath("parent") });
    const notifyPage = await notifyContext.newPage();
    await notifyPage.goto("/notifications");
    await expect(notifyPage.getByRole("heading", { name: /notifications/i })).toBeVisible();
    const ownNotification = notifyPage.locator("li").filter({ hasText: reply });
    const markRead = ownNotification.getByRole("button", { name: /mark read/i });
    await expect(markRead).toBeVisible({ timeout: 30_000 });
    const unreadBefore = await notifyPage.getByRole("button", { name: /mark read/i }).count();
    await markRead.click();
    await expect(markRead).toBeHidden({ timeout: 15_000 });
    await expect(notifyPage.getByRole("button", { name: /mark read/i })).toHaveCount(unreadBefore - 1);
    await expect(ownNotification).toContainText(reply);
    await notifyContext.close();
  });
});
