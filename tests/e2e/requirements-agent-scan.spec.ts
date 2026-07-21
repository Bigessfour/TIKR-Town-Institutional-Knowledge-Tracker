import path from 'node:path';
import { test, expect } from '@playwright/test';
import { gotoClerkPage } from './e2e-helpers';

const txtFixture = path.join(__dirname, '..', 'fixtures', 'agent-scan', 'wiley-periodic-report.txt');

test.describe('Requirements AI Scan', () => {
  test('txt upload pre-fills add requirement dialog', async ({ page }) => {
    await gotoClerkPage(page, '/requirements');
    await expect(page.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible();

    const upload = page.locator('.requirements-agent-upload');
    await expect(upload).toBeVisible();
    // Syncfusion wires browse + change handlers after the control shell mounts.
    await expect(upload.locator('.e-upload-browse-btn')).toBeVisible();
    await expect(upload.locator('input[type="file"]')).toBeAttached();
    await page.waitForTimeout(750);

    await upload.locator('input[type="file"]').setInputFiles(txtFixture);

    // Target the Add requirement Syncfusion dialog (not tour/other dialogs).
    const dialog = page.locator('.e-dialog').filter({ hasText: 'Add requirement' });
    await expect(dialog).toBeVisible({ timeout: 45_000 });
    await expect(page.locator('.ai-suggestion-banner')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Processed on Synology');
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Plain-text extraction');
    await expect(page.getByPlaceholder('Notes, statute cite, filing instructions...')).toHaveValue(
      /Wiley periodic report due Q1 2026/,
      { timeout: 15_000 },
    );
  });
});
