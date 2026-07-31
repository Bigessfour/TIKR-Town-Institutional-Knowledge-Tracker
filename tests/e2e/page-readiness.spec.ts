import path from 'node:path';
import { test, expect, type Page } from '@playwright/test';
import { gotoClerkPage } from './e2e-helpers';

const sampleDocFixture = path.join(
  __dirname,
  '..',
  'fixtures',
  'agent-scan',
  'wiley-periodic-report.txt',
);

async function collectConsoleErrors(page: Page): Promise<string[]> {
  const errors: string[] = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(msg.text());
  });
  return errors;
}

test.describe('TIKR page readiness (nav + primary controls)', () => {
  test.beforeEach(async ({ page }) => {
    collectConsoleErrors(page);
  });

  test('sidebar navigates all clerk routes', async ({ page }) => {
    const routes: { path: string; assert: (p: Page) => Promise<void> }[] = [
      { path: '/', assert: async (p) => await expect(p.getByRole('heading', { name: 'Dashboard' })).toBeVisible() },
      { path: '/calendar', assert: async (p) => await expect(p.getByRole('heading', { name: 'Deadline Calendar' })).toBeVisible() },
      { path: '/requirements', assert: async (p) => await expect(p.getByRole('heading', { name: 'Requirements Manager' })).toBeVisible() },
      { path: '/documents', assert: async (p) => await expect(p.getByRole('heading', { name: 'Document Library' })).toBeVisible() },
      { path: '/assistant', assert: async (p) => await expect(p.getByRole('heading', { name: 'AI Assistant' })).toBeVisible() },
      { path: '/vault', assert: async (p) => await expect(p.getByText(/hit by a bus/i)).toBeVisible() },
      { path: '/settings', assert: async (p) => await expect(p.getByRole('heading', { name: 'Settings' })).toBeVisible() },
    ];

    for (const { path, assert } of routes) {
      await gotoClerkPage(page, path);
      await assert(page);
      await expect(page.getByRole('contentinfo')).toContainText(/Synology|local|NAS|Ollama/i);
    }
  });

  test('dashboard quick actions and footer', async ({ page }) => {
    await gotoClerkPage(page, '/');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
    const main = page.locator('#main-content');
    await expect(main).toBeVisible();
  });

  test('requirements toolbar buttons render and create dialog opens/closes', async ({ page }) => {
    await gotoClerkPage(page, '/requirements');
    await expect(page.getByRole('button', { name: /Add requirement/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Export CSV/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Print council packet/i })).toBeVisible();
    await page.getByRole('button', { name: /Add requirement/i }).click();
    await expect(page.getByRole('dialog')).toBeVisible({ timeout: 15_000 });
    await page.getByRole('button', { name: 'Cancel' }).first().click();
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
  });

  test('documents search mode toggles and uploader region', async ({ page }) => {
    await gotoClerkPage(page, '/documents');
    await expect(page.getByRole('heading', { name: 'Document Library' })).toBeVisible();
    const semantic = page.getByRole('button', { name: /Semantic search/i });
    const full = page.getByRole('button', { name: /Full-text search/i });
    if (await semantic.isVisible()) {
      await semantic.click();
      await full.click();
    }
    await expect(page.locator('.e-upload')).toBeVisible({ timeout: 20_000 });
  });

  test('documents row checkbox selects and shows preview pane', async ({ page }) => {
    await page.setViewportSize({ width: 1600, height: 1000 });
    await gotoClerkPage(page, '/documents');
    await expect(page.getByRole('heading', { name: 'Document Library' })).toBeVisible();

    // Fresh Docker DBs have no seed docs — upload a fixture so selection/preview can run.
    const uploader = page.locator('.uploader-zone');
    await expect(uploader.locator('input[type="file"]')).toBeAttached({ timeout: 20_000 });
    await page.waitForTimeout(500);
    await uploader.locator('input[type="file"]').setInputFiles(sampleDocFixture);

    await expect(page.getByRole('gridcell', { name: /wiley-periodic-report\.txt/i }).first()).toBeVisible({
      timeout: 45_000,
    });
    await page.locator('.e-gridcontent .e-checkbox-wrapper').first().click();
    await expect(page.locator('.preview-pane')).toContainText(
      /wiley-periodic-report|Uploaded|Size|Full text|Type/i,
      { timeout: 15_000 },
    );
  });

  test('vault tabs and copy affordance', async ({ page }) => {
    await gotoClerkPage(page, '/vault');
    await expect(page.getByRole('button', { name: /Copy Everything for New Clerk/i })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'How-To' })).toBeVisible();
    await page.getByRole('tab', { name: 'Voice Notes' }).click();
    await page.getByRole('tab', { name: 'How-To' }).click();
  });

  test('assistant clear button and prompt area', async ({ page }) => {
    await gotoClerkPage(page, '/assistant');
    await expect(page.getByRole('button', { name: /Clear conversation/i })).toBeVisible();
    const prompt = page.locator('.e-aiassistview, .e-assistview, textarea, [contenteditable="true"]').first();
    await expect(prompt).toBeVisible({ timeout: 20_000 });
  });

  test('settings loads API status cards', async ({ page }) => {
    await gotoClerkPage(page, '/settings');
    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
    // Clerk-facing Settings cards (chat memory redesign + helper/storage).
    await expect(page.getByText(/Chat memory/i).first()).toBeVisible();
    await expect(page.getByText('This computer')).toBeVisible();
    await expect(
      page.getByText(/Keys & passwords|Town helper \(local AI\)|Town settings/i).first(),
    ).toBeVisible({ timeout: 15_000 });
  });


  test('calendar grid renders (schedule when Blazor license valid)', async ({ page }) => {
    await gotoClerkPage(page, '/calendar');
    await expect(page.locator('.e-grid')).toBeVisible({ timeout: 25_000 });
    const schedule = page.locator('.e-schedule');
    const licenseMsg = page.getByText(/Schedule view needs a valid Syncfusion Blazor license/i);
    await expect(schedule.or(licenseMsg)).toBeVisible({ timeout: 25_000 });
  });

  test('keyboard help dialog from dashboard', async ({ page }) => {
    await gotoClerkPage(page, '/');
    await page.locator('main').click();
    await page.keyboard.press('?');
    await expect(page.getByRole('dialog', { name: 'Keyboard shortcuts' })).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('no Syncfusion trial overlay on clerk pages', async ({ page }) => {
    for (const path of ['/', '/requirements', '/documents', '/vault']) {
      await gotoClerkPage(page, path);
      const trial = page.getByText(/claim your free account|30-day free trial/i);
      await expect(trial, `trial overlay on ${path}`).toHaveCount(0);
    }
  });
});
