import { test, expect } from '@playwright/test';

test.describe('TIKR clerk smoke', () => {
  test('dashboard loads with local footer', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('contentinfo')).toContainText(/Synology|local/i);
  });

  test('requirements page has help and print affordance', async ({ page }) => {
    await page.goto('/requirements');
    await expect(page.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Print council packet/i })).toBeVisible();
  });
});
