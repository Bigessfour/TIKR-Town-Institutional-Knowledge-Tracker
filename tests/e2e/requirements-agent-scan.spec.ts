import path from 'node:path';
import { test, expect } from '@playwright/test';

const txtFixture = path.join(__dirname, '..', 'fixtures', 'agent-scan', 'wiley-periodic-report.txt');

test.describe('Requirements AI Scan', () => {
  test('txt upload pre-fills add requirement dialog', async ({ page }) => {
    await page.goto('/requirements');
    await expect(page.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible();

    await page.locator('.requirements-agent-upload input[type="file"]').setInputFiles(txtFixture);

    await expect(page.getByRole('dialog', { name: 'Add requirement' })).toBeVisible({ timeout: 30_000 });
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Processed on Synology');
    await expect(page.getByPlaceholder('Notes, statute cite, filing instructions...')).toHaveValue(
      /Wiley periodic report due Q1 2026/,
      { timeout: 15_000 },
    );
  });
});
