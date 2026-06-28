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

  test('keyboard shortcut opens help dialog', async ({ page }) => {
    await page.goto('/');
    await page.waitForFunction(() => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined');
    await page.locator('main').click();
    await page.keyboard.press('?');
    await expect(page.getByRole('dialog', { name: 'Keyboard shortcuts' })).toBeVisible();
    await expect(page.getByText('Go to Requirements')).toBeVisible();
  });
});
