window.tikrShortcuts = {
    _ref: null,
    _handler: null,
    _pendingGo: false,
    _goTimeout: null,
    _routes: {
        d: '/',
        r: '/requirements',
        o: '/documents',
        v: '/vault',
        a: '/assistant',
        s: '/settings'
    },
    register: (dotNetRef) => {
        window.tikrShortcuts._ref?.dispose?.();
        window.tikrShortcuts._ref = dotNetRef;
        if (window.tikrShortcuts._handler)
            document.removeEventListener('keydown', window.tikrShortcuts._handler);

        window.tikrShortcuts._handler = (event) => {
            if (event.target && (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA' || event.target.isContentEditable))
                return;

            if (event.key === '?' || (event.shiftKey && event.key === '/')) {
                event.preventDefault();
                dotNetRef.invokeMethodAsync('OnShortcutsRequested');
                return;
            }

            if (window.tikrShortcuts._pendingGo) {
                window.tikrShortcuts._clearGoPending();
                const route = window.tikrShortcuts._routes[event.key.toLowerCase()];
                if (route) {
                    event.preventDefault();
                    dotNetRef.invokeMethodAsync('OnNavigateRequested', route);
                }
                return;
            }

            if (event.key === 'g' && !event.ctrlKey && !event.metaKey && !event.altKey) {
                window.tikrShortcuts._pendingGo = true;
                window.tikrShortcuts._goTimeout = setTimeout(
                    () => window.tikrShortcuts._clearGoPending(),
                    1500);
            }
        };

        document.addEventListener('keydown', window.tikrShortcuts._handler);
    },
    _clearGoPending: () => {
        window.tikrShortcuts._pendingGo = false;
        if (window.tikrShortcuts._goTimeout) {
            clearTimeout(window.tikrShortcuts._goTimeout);
            window.tikrShortcuts._goTimeout = null;
        }
    },
    dispose: () => {
        window.tikrShortcuts._clearGoPending();
        if (window.tikrShortcuts._handler)
            document.removeEventListener('keydown', window.tikrShortcuts._handler);
        window.tikrShortcuts._handler = null;
        window.tikrShortcuts._ref = null;
    }
};
