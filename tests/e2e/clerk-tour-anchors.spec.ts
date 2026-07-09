import { test, expect } from '@playwright/test';

const ROUTE_ANCHORS: { path: string; tours: string[] }[] = [
  { path: '/', tours: ['nav-dashboard', 'theme-select', 'dashboard-priorities', 'help-dashboard'] },
  { path: '/requirements', tours: ['req-add', 'req-export-csv', 'req-filters', 'req-grid', 'help-requirements'] },
  { path: '/calendar', tours: ['help-calendar', 'cal-schedule'] },
  { path: '/documents', tours: ['doc-uploader', 'doc-search', 'doc-library', 'help-documents'] },
  { path: '/assistant', tours: ['asst-chat', 'asst-advanced', 'help-assistant'] },
  { path: '/vault', tours: ['vault-copy', 'vault-tabs', 'help-vault'] },
  { path: '/settings', tours: ['tour-replay', 'user-guide-open', 'settings-deployment', 'help-settings'] },
];

async function waitForBlazor(page: import('@playwright/test').Page) {
  await page.waitForFunction(
    () => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined',
    { timeout: 45_000 },
  );
}

test.describe('Clerk tour data-tour anchors', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('tikr-tour-completed-version', 'v2');
      localStorage.setItem('tikr-tour-auto-disabled', 'true');
    });
  });

  for (const { path, tours } of ROUTE_ANCHORS) {
    test(`${path} exposes tour anchors`, async ({ page }) => {
      await page.goto(path);
      await waitForBlazor(page);
      for (const id of tours) {
        await expect(page.locator(`[data-tour="${id}"]`).first()).toBeVisible({ timeout: 25_000 });
      }
      await expect(page.getByText('Tour this page', { exact: true })).toBeVisible({ timeout: 15_000 });
    });
  }

  test('settings replay tour opens popover', async ({ page }) => {
    await page.goto('/settings');
    await waitForBlazor(page);
    await page.getByText('Show me around TIKR', { exact: true }).click();
    await expect(page.locator('.tikr-tour-popover')).toBeVisible({ timeout: 15_000 });
    await page.locator('.tikr-tour-skip').click();
    await expect(page.locator('.tikr-tour-popover')).toHaveCount(0, { timeout: 5_000 });
  });
});