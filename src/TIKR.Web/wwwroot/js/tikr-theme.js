window.tikrTheme = {
    get: () => localStorage.getItem('tikr-theme') || 'light',
    set: (theme) => {
        localStorage.setItem('tikr-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    },
    init: () => {
        const theme = window.tikrTheme.get();
        document.documentElement.setAttribute('data-theme', theme);
    }
};

window.tikrTheme.init();
