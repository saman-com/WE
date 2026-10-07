import { test, expect } from "@playwright/test";
import { storagePath } from "../helpers/auth";
import { STUDENT_A, users } from "../helpers/users";

test.describe("g. Access control", () => {
  test("logged-out users are redirected to /login", async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();
    await page.goto("/teacher");
    await expect(page).toHaveURL(/\/login/);
    await context.close();
  });

  const blockedCases: Array<{
    role: keyof typeof users;
    path: string;
    kind: "redirect" | "error";
    error?: RegExp;
  }> = [
    { role: "student", path: "/teacher", kind: "redirect" },
    { role: "teacher", path: "/student", kind: "redirect" },
    { role: "teacher", path: "/parent", kind: "redirect" },
    { role: "student", path: "/leadership", kind: "redirect" },
    { role: "teacher", path: "/authority", kind: "redirect" },
    { role: "teacher", path: "/admin/ai-audit", kind: "redirect" },
    { role: "parent", path: "/admin/federation", kind: "redirect" },
    // Cheap role×route extensions (portal redirects / permission messages — not every API)
    { role: "parent", path: "/teacher", kind: "redirect" },
    { role: "student", path: "/parent", kind: "redirect" },
    { role: "leader", path: "/admin/ai-audit", kind: "redirect" },
    { role: "authority", path: "/organisation", kind: "redirect" },
    {
      role: "student",
      path: "/assessments",
      kind: "error",
      error: /assessment-management permission/i,
    },
    { role: "parent", path: "/organisation", kind: "redirect" },
  ];

  for (const item of blockedCases) {
    test(`${item.role} blocked from ${item.path}`, async ({ browser }) => {
      const context = await browser.newContext({ storageState: storagePath(item.role) });
      const page = await context.newPage();
      await page.goto(item.path);
      if (item.kind === "redirect") {
        await expect(page).toHaveURL(new RegExp(`${users[item.role].home.replace("/", "\\/")}$`));
      } else {
        await expect(page.getByText(item.error!)).toBeVisible();
      }
      await context.close();
    });
  }

  test("system administrator is sent home from leadership", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("admin") });
    const page = await context.newPage();
    await page.goto("/leadership");
    await expect(page).toHaveURL(/\/organisation$/);
    await expect(page.getByRole("heading", { name: "School Leadership Dashboard" })).toHaveCount(0);
    await context.close();
  });

  test("school A teacher cannot see school B student profile data", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("teacher") });
    const page = await context.newPage();
    await page.goto(`/students/${users.studentB.userId}/profile`);
    // Either no-access message or empty/error — must not show School B class enrollment as owned data
    const noAccess = page.getByText(/do not have access|Unable to load|no access/i);
    const schoolB = page.getByText(/School B|11SCI|Year 11 Science/i);
    await expect(noAccess.or(page.getByText(/error|failed/i).first())).toBeVisible({
      timeout: 30_000,
    });
    await expect(schoolB).toHaveCount(0);
    await context.close();
  });

  test("school B teacher does not see school A student in workspace", async ({ browser }) => {
    const context = await browser.newContext({ storageState: storagePath("teacherB") });
    const page = await context.newPage();
    await page.goto("/teacher");
    await expect(page.getByText(STUDENT_A)).toHaveCount(0);
    await expect(page.getByText(/Year 11 Mathematics|11MAT/i)).toHaveCount(0);
    await context.close();
  });
});
