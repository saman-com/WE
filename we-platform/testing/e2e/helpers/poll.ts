import { expect, type Page } from "@playwright/test";

/** Poll a page until the assertion passes or timeout (event-driven fan-out). */
export async function waitForUi(
  page: Page,
  path: string,
  assertion: (page: Page) => Promise<void>,
  options?: { timeoutMs?: number; intervalMs?: number }
) {
  const timeoutMs = options?.timeoutMs ?? 90_000;
  const intervalMs = options?.intervalMs ?? 2_000;
  const deadline = Date.now() + timeoutMs;
  let lastError: unknown;

  while (Date.now() < deadline) {
    await page.goto(path);
    try {
      await assertion(page);
      return;
    } catch (error) {
      lastError = error;
      await page.waitForTimeout(intervalMs);
    }
  }

  throw lastError ?? new Error(`Timed out waiting for UI condition on ${path}`);
}

export async function expectTextSoon(page: Page, path: string, text: string | RegExp) {
  await waitForUi(page, path, async (p) => {
    await expect(p.getByText(text).first()).toBeVisible();
  });
}
