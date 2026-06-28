window.tikrShortcuts = {
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
            }
        };

        document.addEventListener('keydown', window.tikrShortcuts._handler);
    },
    dispose: () => {
        if (window.tikrShortcuts._handler)
            document.removeEventListener('keydown', window.tikrShortcuts._handler);
        window.tikrShortcuts._handler = null;
        window.tikrShortcuts._ref = null;
    }
};
