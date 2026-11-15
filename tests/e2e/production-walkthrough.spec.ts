import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';
import { gotoClerkPage } from './e2e-helpers';

const REPO = path.join(__dirname, '..', '..');

test.describe('Production walkthrough findings (main e1c5399)', () => {
  test('RB-006/RB-003 assistant: immediate progress + answer budget (stub stack)', async ({ page }) => {
    test.fail(true, 'RB-006/RB-003 production timeout + latency');
    await gotoClerkPage(page, '/assistant');
    const prompt = page.locator('.e-aiassistview textarea, .e-aiassistview [contenteditable="true"]').first();
    await expect(prompt).toBeVisible({ timeout: 25_000 });
    const t0 = Date.now();
    await prompt.fill('List open due-outs');
    await page.keyboard.press('Enter');
    await expect(page.getByText(/preparing|looking through|thinking/i)).toBeVisible({ timeout: 2_000 });
    await expect(page.locator('.e-aiassistview')).not.toContainText('Response stopped', { timeout: 45_000 });
    expect(Date.now() - t0).toBeLessThan(45_000);
  });

  test('RB-006 assistant timeout handling does not escalate Grok on cancel', () => {
    const razor = fs.readFileSync(path.join(REPO, 'src/TIKR.Web/Components/Pages/Assistant.razor'), 'utf8');
    expect(razor).toContain('Response stopped');
    expect(razor).not.toMatch(/catch \(OperationCanceledException\)[\s\S]{0,500}CompleteViaGrokAsync/);
  });

  test('RB-008 guide must not document Ask Advanced button', () => {
    test.fail(true, 'RB-008');
    const md = fs.readFileSync(path.join(REPO, 'src/TIKR.Web/wwwroot/help/clerk-user-guide.md'), 'utf8');
    expect(md).not.toMatch(/Ask Advanced AI/);
    const tour = fs.readFileSync(path.join(REPO, 'src/TIKR.Web/ClerkTour/ClerkTourCatalog.cs'), 'utf8');
    expect(tour).not.toMatch(/Ask Advanced AI/);
  });

  test('RB-020 release: auth required in production deployments', () => {
    test.fail(true, 'RB-020');
    const routes = fs.readFileSync(path.join(REPO, 'src/TIKR.Web/Components/Routes.razor'), 'utf8');
    expect(routes).not.toMatch(/else\s*\{\s*<RouteView/);
  });
});