import { test } from '@playwright/test';
import {
  annotateKnownFailure,
  exerciseInventoryItem,
  functionsInventory,
  inventoryItemsForRun,
} from './inventory-runner';
import { gotoClerkPage } from './e2e-helpers';

const items = inventoryItemsForRun();
const byRoute = new Map<string, typeof items>();

for (const item of items) {
  const route =
    item.route && item.route !== '*' && item.route !== 'unknown'
      ? item.route
      : item.type === 'route'
        ? item.route
        : '/';
  const key = route || '/';
  const list = byRoute.get(key) ?? [];
  list.push(item);
  byRoute.set(key, list);
}

test.describe('Functions inventory (per control)', () => {
  for (const [route, routeItems] of byRoute) {
    test.describe(`route ${route}`, () => {
      test.beforeEach(async ({ page }) => {
        if (route.startsWith('/')) {
          await gotoClerkPage(page, route);
        }
      });

      for (const item of routeItems) {
        test(`${item.testId} | ${item.label}`, async ({ page }, testInfo) => {
          annotateKnownFailure(testInfo, item);

          if (item.knownFailure) {
            test.fail(true, `${item.knownFailure}: ${item.expectedResult}`);
          }

          if (item.writesData && (process.env.TIKR_E2E_SMOKE_MODE === '1' || process.env.TIKR_E2E_SKIP_WRITES === '1')) {
            test.skip(true, 'writesData skipped in smoke/write-safe mode');
          }

          await exerciseInventoryItem(page, item);
        });
      }
    });
  }
});

test('inventory meta snapshot', async () => {
  test.info().annotations.push({
    type: 'inventory-count',
    description: JSON.stringify(functionsInventory.meta),
  });
});