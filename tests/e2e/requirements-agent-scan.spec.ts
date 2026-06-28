import path from 'node:path';
import { test, expect } from '@playwright/test';

const txtFixture = path.join(__dirname, '..', 'fixtures', 'agent-scan', 'wiley-periodic-report.txt');

test.describe('Requirements AI Scan', () => {
  test('txt upload pre-fills add requirement dialog', async ({ page }) => {
    test.skip(
      !process.env.SYNCFUSION_LICENSE_KEY,
      'Set SYNCFUSION_LICENSE_KEY on the Web container (valid license) to run dialog UI test',
    );

    await page.goto('/requirements');
    await expect(page.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible();
    await page.waitForFunction(() => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined');

    await page.locator('.requirements-agent-upload input[type="file"]').setInputFiles(txtFixture);

    await expect(page.locator('.ai-suggestion-banner')).toBeVisible({ timeout: 30_000 });
    await expect(page.locator('.e-dialog')).toBeVisible();
    await expect(page.locator('.e-dlg-header-content')).toContainText('Add requirement');
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Processed on Synology');
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Plain-text extraction');
    await expect(page.getByPlaceholder('Notes, statute cite, filing instructions...')).toHaveValue(
      /Wiley periodic report due Q1 2026/,
      { timeout: 15_000 },
    );
  });
});
