import { defineConfig, devices } from '@playwright/test';

/**
 * Configuration reserved for the manually controlled P06 QA runner.
 * It intentionally selects only the future real-auth suite and emits no
 * screenshots, videos, traces, HAR files, HTML reports, or storage state.
 */
export default defineConfig({
  testDir: './e2e',
  testMatch: '**/p06-qa-real-auth.spec.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  reporter: [['line']],
  outputDir: 'test-results-p06-qa',
  preserveOutput: 'never',
  use: {
    ...devices['Desktop Chrome'],
    baseURL: process.env['P06_QA_WEB_ORIGIN'] ?? 'http://localhost:4200',
    screenshot: 'off',
    trace: 'off',
    video: 'off',
    serviceWorkers: 'block',
  },
});
