window.tikrTheme = {
    get: () => {
        try { return localStorage.getItem('tikr-theme') || 'light'; } catch { return 'light'; }
    },
    set: (theme) => {
        try {
            localStorage.setItem('tikr-theme', theme);
            if (document && document.documentElement) {
                document.documentElement.setAttribute('data-theme', theme);
            }
            if (document && document.body) {
                document.body.setAttribute('data-tikr-theme', theme);
            }
            switchSyncfusionTheme(theme);
        } catch (e) {
            // Never let theme switch crash the Blazor circuit (would show reload banner)
            console.warn('tikrTheme.set failed (non-fatal)', e);
        }
    },
    init: () => {
        try {
            const theme = window.tikrTheme.get();
            if (document && document.documentElement) {
                document.documentElement.setAttribute('data-theme', theme);
            }
            if (document && document.body) {
                document.body.setAttribute('data-tikr-theme', theme);
            }
            switchSyncfusionTheme(theme);
        } catch (e) {
            console.warn('tikrTheme.init failed (non-fatal)', e);
        }
    }
};

function switchSyncfusionTheme(theme) {
    try {
        const link = document.getElementById('syncfusion-theme');
        if (!link) return;
        let css = 'bootstrap5';
        if (theme === 'dark') css = 'bootstrap5-dark';
        else if (theme === 'high-contrast') css = 'highcontrast';
        // Use Syncfusion theme bundle for proper control theming (fetched via Syncfusion MCP guidance for blazor theme switching)
        link.href = `_content/Syncfusion.Blazor.Themes/${css}.css`;
    } catch (e) {
        // Theme CSS swap is best-effort; custom CSS + data-theme attributes still apply
    }
}

// Defer slightly in case of script ordering with Blazor interactive render
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => window.tikrTheme.init());
} else {
    window.tikrTheme.init();
}
