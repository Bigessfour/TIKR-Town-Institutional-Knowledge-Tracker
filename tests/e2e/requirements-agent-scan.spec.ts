import path from 'node:path';
import { test, expect } from '@playwright/test';

const txtFixture = path.join(__dirname, '..', 'fixtures', 'agent-scan', 'wiley-periodic-report.txt');

test.describe('Requirements AI Scan', () => {
  test('txt upload pre-fills add requirement dialog', async ({ page }) => {
    await page.goto('/requirements');
    await expect(page.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible();
    await page.waitForFunction(
      () => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined',
      { timeout: 45_000 },
    );

    const upload = page.locator('.requirements-agent-upload');
    await expect(upload).toBeVisible();

    // Syncfusion SfUploader: open native file chooser via Browse (setInputFiles on hidden input is unreliable).
    const [fileChooser] = await Promise.all([
      page.waitForEvent('filechooser'),
      upload.getByRole('button', { name: /Browse/i }).click(),
    ]);
    await fileChooser.setFiles(txtFixture);

    await expect(page.getByRole('dialog')).toBeVisible({ timeout: 45_000 });
    await expect(page.locator('.e-dlg-header-content')).toContainText('Add requirement');
    await expect(page.locator('.ai-suggestion-banner')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Processed on Synology');
    await expect(page.locator('.ai-suggestion-banner')).toContainText('Plain-text extraction');
    await expect(page.getByPlaceholder('Notes, statute cite, filing instructions...')).toHaveValue(
      /Wiley periodic report due Q1 2026/,
      { timeout: 15_000 },
    );
  });
});