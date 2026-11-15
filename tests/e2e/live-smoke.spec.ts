/**
 * Optional read-only smoke against a live deployment (e.g. http://mr-storage:8080).
 * Enable: TIKR_E2E_SMOKE_MODE=1 TIKR_E2E_BASE_URL=http://mr-storage:8080 npm test -- live-smoke
 */
import { test, expect } from '@playwright/test';
import { gotoClerkPage } from './e2e-helpers';
import { functionsInventory, inventoryItemsForRun } from './inventory-runner';

test.describe('Live smoke (navigation + read-only)', () => {
  test.beforeAll(() => {
    test.skip(
      process.env.TIKR_E2E_SMOKE_MODE !== '1' && process.env.TIKR_E2E_SMOKE_MODE !== 'true',
      'Set TIKR_E2E_SMOKE_MODE=1 to run live smoke',
    );
  });

  test('all clerk routes respond', async ({ page }) => {
    const routes = functionsInventory.items
      .filter((i) => i.type === 'route' && i.route.startsWith('/'))
      .map((i) => i.route);
  const unique = [...new Set(routes)].filter((r) => r !== '/knowledge' && r !== '/not-found' && r !== '/Error');

    for (const route of unique) {
      await gotoClerkPage(page, route);
      await expect(page.locator('#main-content, main').first()).toBeVisible({ timeout: 45_000 });
    }
  });

  test('smoke-safe inventory count matches filter', async () => {
    const smoke = inventoryItemsForRun();
    expect(smoke.length).toBeGreaterThan(50);
    expect(smoke.every((i) => i.smokeSafe && !i.writesData)).toBe(true);
  });
});