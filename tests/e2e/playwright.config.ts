import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  timeout: 120_000,
  workers: process.env.TIKR_E2E_SMOKE_MODE ? 2 : 1,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI
    ? [
        ['list'],
        ['json', { outputFile: 'test-results/inventory-results.json' }],
      ]
    : [['list']],
  use: {
    baseURL: process.env.TIKR_E2E_BASE_URL ?? 'http://localhost:8080',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'clerk-core',
      testMatch: /^(?!functions-inventory|live-smoke).*\.spec\.ts$/,
    },
    {
      name: 'functions-inventory',
      testMatch: /functions-inventory\.spec\.ts$/,
      timeout: 180_000,
    },
    {
      name: 'live-smoke',
      testMatch: /live-smoke\.spec\.ts$/,
    },
  ],
});
