import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { gotoClerkPage } from './e2e-helpers';

/**
 * Accessibility smoke — fails only on critical axe violations in our UI.
 * Syncfusion control chrome (grid pager/clipboard, etc.) is excluded: vendor
 * ARIA is not fixable here and serious/moderate findings stay log-only.
 */
test.describe('Accessibility smoke (axe)', () => {
  const routes = ['/', '/requirements', '/documents', '/vault', '/settings', '/calendar'];

  for (const route of routes) {
    test(`no critical axe violations on ${route}`, async ({ page }) => {
      await gotoClerkPage(page, route);
      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa'])
        // Syncfusion DataGrid embeds pager + hidden clipboard inside role=grid
        // (aria-required-children). Scope out vendor control roots only.
        .exclude('.e-grid')
        .exclude('.sf-grid')
        .exclude('.e-schedule')
        .exclude('.e-treeview')
        .exclude('.e-tab')
        .exclude('.e-upload')
        .exclude('.e-aiassistview')
        .exclude('.e-assistview')
        .analyze();

      const critical = results.violations.filter((v) => v.impact === 'critical');
      if (results.violations.length > 0) {
        console.log(
          `axe ${route}: ${results.violations.length} issue(s)`,
          results.violations.map((v) => `${v.impact}:${v.id}`).join(', '),
        );
      }

      expect(critical, JSON.stringify(critical, null, 2)).toEqual([]);
    });
  }
});
