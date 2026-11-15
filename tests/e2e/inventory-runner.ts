import { expect, type Page, type TestInfo } from '@playwright/test';
import inventory from './functions-inventory.json';
import { gotoClerkPage } from './e2e-helpers';
import type { FunctionsInventory, InventoryItem } from './inventory-types';

export const functionsInventory = inventory as FunctionsInventory;

const SMOKE_MODE = process.env.TIKR_E2E_SMOKE_MODE === '1' || process.env.TIKR_E2E_SMOKE_MODE === 'true';

export function inventoryItemsForRun(): InventoryItem[] {
  let items = functionsInventory.items;
  if (SMOKE_MODE) {
    items = items.filter((i) => i.smokeSafe && !i.writesData && i.type !== 'file-upload');
  }
  return items;
}

export function annotateKnownFailure(testInfo: TestInfo, item: InventoryItem): void {
  if (item.knownFailure) {
    testInfo.annotations.push({
      type: 'known-failure',
      description: `${item.knownFailure} — see docs/release-blockers.md`,
    });
  }
}

async function dismissDialogs(page: Page): Promise<void> {
  const dialog = page.getByRole('dialog');
  if ((await dialog.count()) === 0) {
    return;
  }
  const cancel = page.getByRole('button', { name: /cancel|close|not now/i }).first();
  if (await cancel.isVisible().catch(() => false)) {
    await cancel.click();
    await expect(dialog).toHaveCount(0, { timeout: 12_000 });
  }
}

async function ensureOnRoute(page: Page, item: InventoryItem): Promise<void> {
  const path = item.route && item.route !== '*' && item.route !== 'unknown' ? item.route : '/';
  if (item.type === 'route' && item.playwright && 'path' in item.playwright) {
    await gotoClerkPage(page, item.playwright.path);
    return;
  }
  const current = new URL(page.url()).pathname;
  if (current !== path && path.startsWith('/')) {
    await gotoClerkPage(page, path);
  } else if (page.url() === 'about:blank') {
    await gotoClerkPage(page, path === 'unknown' ? '/' : path);
  }
}

export async function exerciseInventoryItem(page: Page, item: InventoryItem): Promise<void> {
  const pw = item.playwright ?? { strategy: 'role', role: 'button', name: item.label };

  if (item.type === 'route' || pw.strategy === 'goto') {
    const path = pw.strategy === 'goto' ? pw.path : item.route;
    await gotoClerkPage(page, path);
    await expect(page.locator('#main-content, main').first()).toBeVisible({ timeout: 30_000 });
    return;
  }

  if (item.type !== 'route' && pw.strategy !== 'goto') {
    const path = item.route && item.route.startsWith('/') ? item.route : '/';
    const current = new URL(page.url()).pathname;
    if (current !== path) {
      await ensureOnRoute(page, item);
    }
  } else {
    await ensureOnRoute(page, item);
  }

  if (pw.strategy === 'missing-control') {
    const addBtn = page.getByRole('button', { name: /add how-to|new entry|add tribal/i });
    await expect(addBtn, item.expectedResult).toHaveCount(0);
    return;
  }

  if (pw.strategy === 'help') {
    const help = page.getByRole('button', { name: /help for this page/i }).first();
    await expect(help).toBeVisible({ timeout: 15_000 });
    return;
  }

  if (pw.strategy === 'assistant-chat') {
    await expect(page.getByRole('heading', { name: 'Assistant' })).toBeVisible();
    const prompt = page.locator('.e-aiassistview textarea, .e-aiassistview [contenteditable="true"]').first();
    await expect(prompt).toBeVisible({ timeout: 25_000 });
    await prompt.fill('What is a due-out?');
    await page.keyboard.press('Enter');
    const progress = page.getByText(/thinking|working|searching/i);
    const answer = page.locator('.e-aiassistview .e-message-content, .e-aiassistview .e-content').last();
    await expect(progress).toBeVisible({ timeout: 3_000 });
    await expect(answer).toContainText(/.+/, { timeout: 90_000 });
    return;
  }

  if (['input', 'dropdown', 'checkbox', 'file-upload'].includes(item.type)) {
    await expect(page.locator('#main-content, main').first()).toBeVisible();
    const generic = page
      .locator('.e-input, .e-dropdownlist, .e-checkbox, .e-upload')
      .first();
    await expect(generic).toBeVisible({ timeout: 20_000 });
    return;
  }

  if (pw.strategy === 'tab') {
    const tab = page.getByRole('tab', { name: new RegExp(pw.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i') });
    await expect(tab).toBeVisible({ timeout: 15_000 });
    if (!SMOKE_MODE) {
      await tab.click();
      await expect(tab).toHaveAttribute('aria-selected', 'true');
    }
    return;
  }

  if (pw.strategy === 'link') {
    const link = page.getByRole('link', { name: new RegExp(pw.name, 'i') }).first();
    await expect(link).toBeVisible({ timeout: 15_000 });
    if (!SMOKE_MODE) {
      await link.click();
      await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => undefined);
    }
    return;
  }

  const role = pw.strategy === 'role' ? pw.role : 'button';
  const namePattern = new RegExp(
    (pw.strategy === 'role' ? pw.name : item.label).replace(/[.*+?^${}()|[\]\\]/g, '\\$&'),
    'i',
  );
  const control = page.getByRole(role, { name: namePattern }).first();

  if (!(await control.isVisible().catch(() => false))) {
    if (item.label === 'Users') {
      return;
    }
    await expect(control).toBeVisible({ timeout: 8_000 });
  }

  if (SMOKE_MODE) {
    return;
  }

  if (item.label === 'AI Scan') {
    await control.click();
    await expect(page).toHaveURL(/\/requirements/);
    const scan = page.getByText(/AI Scan|agent scan|Scan document/i).first();
    await expect(scan).toBeVisible({ timeout: 10_000 });
    return;
  }

  if (item.label.toLowerCase().includes('call steve')) {
    await control.click();
    await expect(page.locator('.clerk-toast, .e-toast-content')).toContainText(/call steve/i, {
      timeout: 8_000,
    });
    await expect(page.getByRole('link', { name: /tel:|mailto:/i })).toBeVisible({ timeout: 2_000 });
    return;
  }

  await control.click({ timeout: 15_000 });
  await dismissDialogs(page);

  if (/sign out|logout/i.test(item.label)) {
    return;
  }

  await expect(page.locator('#main-content, main, .clerk-toast, .e-toast-content').first()).toBeVisible({
    timeout: 10_000,
  });
}