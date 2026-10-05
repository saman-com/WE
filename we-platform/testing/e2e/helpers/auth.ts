import { type Browser, type BrowserContext, type Page, expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { DEMO_PASSWORD, type UserKey, users } from "./users";

const authDir = path.join(__dirname, "..", ".auth");

export function storagePath(key: UserKey): string {
  return path.join(authDir, users[key].storageFile);
}

export async function loginViaUi(page: Page, email: string, password = DEMO_PASSWORD) {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  await expect
    .poll(async () => page.evaluate(() => localStorage.getItem("we_access_token")))
    .not.toBeNull();
}

export async function ensureAuthDir() {
  fs.mkdirSync(authDir, { recursive: true });
}

export async function withRole(
  browser: Browser,
  key: UserKey,
  run: (page: Page, context: BrowserContext) => Promise<void>
) {
  const context = await browser.newContext({ storageState: storagePath(key) });
  const page = await context.newPage();
  try {
    await run(page, context);
  } finally {
    await context.close();
  }
}
