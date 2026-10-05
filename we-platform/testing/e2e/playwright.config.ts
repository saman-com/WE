import { defineConfig, devices } from "@playwright/test";
import path from "node:path";

const PORT = Number(process.env.E2E_PORT ?? 3000);
// Identity CORS allows http://localhost:3000 (not 127.0.0.1).
const baseURL = process.env.E2E_BASE_URL ?? `http://localhost:${PORT}`;
export default defineConfig({

  testDir: "./tests",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  maxFailures: 100,
  timeout: 120_000,
  expect: { timeout: 30_000 },
  reporter: [
    ["list"],
    ["html", { open: "never", outputFolder: "playwright-report" }],
  ],
  outputDir: "test-results",
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
  },
  ...(process.env.E2E_SKIP_WEBSERVER === "1"
    ? {}
    : {
        webServer: {
          // Invoke next directly: packageManager-pinned `pnpm exec` can fail when the
          // store-linked pnpm binary is missing from this environment.
          command: `./node_modules/.bin/next dev --port ${PORT} --hostname localhost`,
          cwd: path.join(__dirname, "../../apps/web-portal"),
          url: baseURL,
          reuseExistingServer: !process.env.CI,
          timeout: 180_000,
          env: {
            ...process.env,
            PORT: String(PORT),
            WATCHPACK_POLLING: "true",
            CHOKIDAR_USEPOLLING: "true",
          },
        },
      }),
  projects: [
    {
      name: "setup",
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: "chromium",
      dependencies: ["setup"],
      use: {
        ...devices["Desktop Chrome"],
      },
      testIgnore: /auth\.setup\.ts/,
    },
  ],
});
