import { test as setup } from "@playwright/test";
import { ensureAuthDir, loginViaUi, storagePath } from "../helpers/auth";
import { type UserKey, users } from "../helpers/users";

const roles = Object.keys(users) as UserKey[];

setup("authenticate all demo roles", async ({ page }) => {
  await ensureAuthDir();

  for (const key of roles) {
    await loginViaUi(page, users[key].email);
    await page.context().storageState({ path: storagePath(key) });
    await page.evaluate(() => localStorage.clear());
    await page.context().clearCookies();
  }
});
