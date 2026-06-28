import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  timeout: 60_000,
  retries: process.env.CI ? 1 : 0,
  use: {
    baseURL: process.env.TIKR_E2E_BASE_URL ?? 'http://localhost:8080',
    trace: 'on-first-retry',
  },
});
