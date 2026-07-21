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

export async function gotoClerkPage(page: Page, path: string): Promise<void> {
  await disableClerkTour(page);
  await page.goto(path);
  await page.waitForFunction(
    () => typeof (window as unknown as { sfBlazor?: unknown }).sfBlazor !== 'undefined',
    { timeout: 45_000 },
  );
  await dismissClerkTourIfPresent(page);
}
