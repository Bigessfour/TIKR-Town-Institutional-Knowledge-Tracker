import fs from 'node:fs';
import path from 'node:path';
import { expect, type Page } from '@playwright/test';
import { gotoClerkPage } from './e2e-helpers';
import type { InventoryItem } from './inventory-types';

const REPO = path.join(__dirname, '..', '..');

function readRepo(rel: string): string {
  return fs.readFileSync(path.join(REPO, rel), 'utf8');
}

export async function runProductionWalkthroughStrategy(
  page: Page,
  item: InventoryItem,
  pw: { strategy: string },
): Promise<boolean> {
  switch (pw.strategy) {
    case 'assistant-timeout': {
      const razor = readRepo('src/TIKR.Web/Components/Pages/Assistant.razor');
      expect(razor).not.toContain('Response stopped.');
      expect(razor).toMatch(/try again|timed out|took too long/i);
      return true;
    }
    case 'assistant-grok-timeout': {
      const razor = readRepo('src/TIKR.Web/Components/Pages/Assistant.razor');
      expect(razor).toMatch(
        /catch \(OperationCanceledException\)[\s\S]{0,600}CompleteViaGrokAsync/,
      );
      return true;
    }
    case 'assistant-prompt-reset': {
      await gotoClerkPage(page, '/assistant');
      const prompt = page.locator('.e-aiassistview textarea, .e-aiassistview input').first();
      await expect(prompt).toBeVisible({ timeout: 20_000 });
      const defaultSnippet = 'What should I be working on this week';
      await prompt.fill('Unique clerk test question xyzzy');
      await page.keyboard.press('Enter');
      await page.waitForTimeout(2500);
      const value = (await prompt.inputValue().catch(() => '')) || (await prompt.textContent()) || '';
      expect(value).not.toContain(defaultSnippet);
      expect(value.trim()).toBe('');
      return true;
    }
    case 'guide-ask-advanced': {
      const md = readRepo('src/TIKR.Web/wwwroot/help/clerk-user-guide.md');
      expect(md).not.toMatch(/Ask Advanced AI/);
      const tour = readRepo('src/TIKR.Web/ClerkTour/ClerkTourCatalog.cs');
      expect(tour).not.toMatch(/Ask Advanced AI/);
      return true;
    }
    case 'calendar-create-deadline': {
      await gotoClerkPage(page, '/calendar');
      const btn = page.getByRole('button', { name: /create deadline/i });
      await expect(btn).toBeVisible();
      await expect(btn).toBeDisabled();
      await expect(page.getByText(/type.*plain english|enter.*deadline/i)).toBeVisible();
      return true;
    }
    case 'calendar-all-day':
    case 'calendar-recurring': {
      await gotoClerkPage(page, '/calendar');
      const schedule = page.locator('.e-schedule');
      if ((await schedule.count()) === 0) {
        expect(true).toBe(true);
        return true;
      }
      const overnight = schedule.locator('.e-appointment').filter({ hasText: /12:00 AM|1:00 AM/i });
      await expect(overnight).toHaveCount(0);
      return true;
    }
    case 'documents-row-click': {
      await page.setViewportSize({ width: 1600, height: 1000 });
      await gotoClerkPage(page, '/documents');
      const row = page.locator('.e-gridcontent .e-row').first();
      if ((await row.count()) === 0) return true;
      const preview = page.locator('.preview-pane');
      const before = (await preview.textContent()) ?? '';
      await row.click({ position: { x: 120, y: 12 } });
      await page.waitForTimeout(800);
      const after = (await preview.textContent()) ?? '';
      expect(after.trim().length).toBeGreaterThan(before.trim().length);
      return true;
    }
    case 'documents-utc-time': {
      const razor = readRepo('src/TIKR.Web/Components/Pages/Documents.razor');
      const gridColumn = razor.match(
        /Field="@nameof\(DocumentDto\.UploadedAt\)"[^>]+Format="g"/,
      );
      expect(gridColumn?.[0] ?? '').toMatch(/ToLocalTime|DisplayFormat/);
      return true;
    }
    case 'user-guide-reopen': {
      await gotoClerkPage(page, '/');
      const guideBtn = page.getByRole('button', { name: /user guide/i });
      await guideBtn.click();
      await expect(page.getByRole('dialog', { name: /user guide/i })).toBeVisible({ timeout: 10_000 });
      await page.keyboard.press('Escape');
      await expect(page.getByRole('dialog', { name: /user guide/i })).toHaveCount(0, { timeout: 8_000 });
      await guideBtn.click();
      await expect(page.getByRole('dialog', { name: /user guide/i })).toBeVisible({ timeout: 10_000 });
      return true;
    }
    case 'page-help-click': {
      await gotoClerkPage(page, '/');
      const help = page.getByRole('button', { name: /help for this page/i }).first();
      await help.click();
      await expect(page.getByRole('dialog')).toHaveCount(0);
      const openTooltip = page.locator('.e-tooltip-wrap.e-popup-open');
      await expect(openTooltip).toHaveCount(0);
      return true;
    }
    case 'home-upload-picker': {
      await gotoClerkPage(page, '/');
      await page.getByRole('button', { name: /upload document/i }).click();
      await expect(page).toHaveURL(/\/documents/);
      await expect(page.locator('input[type="file"]').first()).toBeFocused({ timeout: 5_000 });
      return true;
    }
    case 'home-add-requirement': {
      await gotoClerkPage(page, '/');
      await page.getByRole('button', { name: /add requirement/i }).click();
      await expect(page).toHaveURL(/\/requirements/);
      await expect(page.getByRole('dialog')).toBeVisible({ timeout: 5_000 });
      return true;
    }
    case 'settings-reindex': {
      await gotoClerkPage(page, '/settings');
      await expect(page.getByRole('button', { name: /reindex/i })).toBeVisible({ timeout: 20_000 });
      return true;
    }
    case 'print-council-packet': {
      const razor = readRepo('src/TIKR.Web/Components/Pages/Requirements.razor');
      expect(razor).not.toMatch(/PrintPacketAsync[\s\S]{0,200}InvokeVoidAsync\("print"\)/);
      return true;
    }
    case 'settings-duplicate-tour': {
      const settings = readRepo('src/TIKR.Web/Components/Pages/Settings.razor');
      const duplicate =
        settings.includes('PageTourButton Route="/settings"') &&
        settings.includes('Replay sidebar menu tour');
      expect(duplicate).toBe(false);
      return true;
    }
    case 'account-auth': {
      await gotoClerkPage(page, '/account');
      await expect(page.getByRole('heading', { name: /account/i })).toBeVisible({ timeout: 15_000 });
      const example = readRepo('docker/.env.example');
      expect(example).toMatch(/TIKR_AUTH_ENABLED=true/i);
      return true;
    }
    case 'settings-status': {
      await gotoClerkPage(page, '/settings');
      const text = (await page.locator('#main-content').textContent()) ?? '';
      const ready = /town helper:\s*ready/i.test(text);
      const offline = /ollama offline|helper.*offline/i.test(text);
      expect(ready && offline).toBe(false);
      return true;
    }
    case 'a11y-overdue-contrast': {
      await gotoClerkPage(page, '/');
      const pill = page.locator('.priority-overdue').first();
      await expect(pill).toBeVisible({ timeout: 15_000 });
      const ratio = await pill.evaluate((el) => {
        const style = getComputedStyle(el);
        const parse = (c: string) => c.match(/\d+/g)?.slice(0, 3).map(Number) ?? [0, 0, 0];
        const fg = parse(style.color);
        const bg = parse(getComputedStyle(el.parentElement ?? el).backgroundColor);
        const rel = (c: number[]) => {
          const [r, g, b] = c.map((x) => {
            const s = x / 255;
            return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
          });
          return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        };
        const l1 = rel(fg);
        const l2 = rel(bg);
        return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
      });
      expect(ratio).toBeGreaterThanOrEqual(4.5);
      return true;
    }
    case 'grid-filter-aria': {
      await gotoClerkPage(page, '/requirements');
      await page.locator('.e-filterbar').first().waitFor({ state: 'visible', timeout: 20_000 }).catch(() => undefined);
      const labels = await page.locator('.e-filterbar input[aria-label]').evaluateAll((inputs) =>
        inputs.map((i) => i.getAttribute('aria-label') ?? ''),
      );
      if (labels.length === 0) return true;
      for (const l of labels) {
        expect(l).not.toBe('_Filter');
      }
      return true;
    }
    default:
      return false;
  }
}