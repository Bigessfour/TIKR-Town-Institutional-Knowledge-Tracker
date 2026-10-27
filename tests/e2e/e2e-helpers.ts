import { type Page, expect } from '@playwright/test';

/** Keep in sync with ClerkTourCatalog.CurrentVersion */
export const CLERK_TOUR_COMPLETED_VERSION = 'v2';

/**
 * Prevent auto-start clerk tour and clear any leftover overlay DOM.
 * Call before navigating, or after goto when Blazor has loaded.
 */
export async function disableClerkTour(page: Page): Promise<void> {
  await page.addInitScript(
    ({ completedKey, autoDisabledKey, version }) => {
      try {
        localStorage.setItem(completedKey, version);
        localStorage.setItem(autoDisabledKey, 'true');
      } catch {
        /* ignore */
      }
    },
    {
      completedKey: 'tikr-tour-completed-version',
      autoDisabledKey: 'tikr-tour-auto-disabled',
      version: CLERK_TOUR_COMPLETED_VERSION,
    },
  );
}

export async function dismissClerkTourIfPresent(page: Page): Promise<void> {
  const skip = page.locator('button.tikr-tour-skip');
  if (await skip.isVisible().catch(() => false)) {
    await skip.click();
  }

  await page.evaluate(() => {
    const w = window as unknown as { tikrTour?: { _destroy?: () => void } };
    w.tikrTour?._destroy?.();
    document
      .querySelectorAll('.tikr-tour-overlay, .tikr-tour-spotlight, .tikr-tour-popover')
      .forEach((n) => n.remove());
  });

  await expect(page.locator('.tikr-tour-overlay')).toHaveCount(0, { timeout: 5_000 });
}

/**
 * Dependabot pull requests do not receive Actions secrets, so CI runs this
 * suite without SYNCFUSION_LICENSE_KEY. Syncfusion then injects a trial banner
 * that covers the page and fails axe. Remove that banner only on the
 * unlicensed path. A licensed run must still fail if the banner appears.
 */
export async function dismissSyncfusionTrialIfPresent(page: Page): Promise<void> {
  if (process.env.SYNCFUSION_LICENSE_KEY) {
    return;
  }

  await page.evaluate(() => {
    const trial = /claim your free account|30-day free trial/i;
    const nodes = Array.from(document.querySelectorAll('body *'));
    for (const el of nodes) {
      const text = el.textContent ?? '';
      if (!trial.test(text) || text.length > 800) {
        continue;
      }
      let node: HTMLElement | null = el as HTMLElement;
      let overlay: HTMLElement | null = null;
      while (node && node !== document.body) {
        if (node.querySelector('main')) {
          break;
        }
        const style = window.getComputedStyle(node);
        if (style.position === 'fixed' || style.position === 'sticky' || node.parentElement === document.body) {
          overlay = node;
        }
        node = node.parentElement;
      }
      overlay?.remove();
    }

    document.querySelectorAll('body > img[src^="data:image"]').forEach((img) => img.remove());
  });
}

export async function gotoClerkPage(page: Page, path: string): Promise<void> {
  await disableClerkTour(page);
  await page.goto(path);
  await page.waitForFunction(
    () => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined',
    { timeout: 45_000 },
  );
  await page.waitForFunction(
    () =>
      typeof (window as unknown as { tikrShortcuts?: { _handler?: unknown } }).tikrShortcuts
        ?._handler === 'function',
    { timeout: 15_000 },
  );
  await dismissClerkTourIfPresent(page);
  await dismissSyncfusionTrialIfPresent(page);
}
