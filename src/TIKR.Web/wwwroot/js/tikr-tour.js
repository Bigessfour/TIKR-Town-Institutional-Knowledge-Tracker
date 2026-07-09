window.tikrTour = {
    getLocalValue(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    setLocalValue(key, value) {
        try { localStorage.setItem(key, value); } catch { /* ignore */ }
    },
    getLocalFlag(key) {
        try { return localStorage.getItem(key) === 'true'; } catch { return false; }
    },
    setLocalFlag(key, value) {
        try { localStorage.setItem(key, value ? 'true' : 'false'); } catch { /* ignore */ }
    },

    async run(steps, dotNetBridge) {
        if (!steps || steps.length === 0) return;
        tikrTour._destroy();

        const overlay = document.createElement('div');
        overlay.className = 'tikr-tour-overlay';
        overlay.setAttribute('role', 'presentation');
        const spotlight = document.createElement('div');
        spotlight.className = 'tikr-tour-spotlight';
        const popover = document.createElement('div');
        popover.className = 'tikr-tour-popover';
        popover.setAttribute('role', 'dialog');
        popover.setAttribute('aria-modal', 'true');
        document.body.appendChild(overlay);
        document.body.appendChild(spotlight);
        document.body.appendChild(popover);

        let index = 0;
        const state = { active: true };

        const finish = async () => {
            if (!state.active) return;
            state.active = false;
            tikrTour._destroy();
            if (dotNetBridge && dotNetBridge.invokeMethodAsync) {
                try {
                    await dotNetBridge.invokeMethodAsync('OnTourFinishedFromJs');
                } catch { /* circuit gone */ }
            }
        };

        const waitMs = (ms) => new Promise((r) => setTimeout(r, ms));

        const waitForElement = async (selector, timeoutMs) => {
            const end = Date.now() + timeoutMs;
            while (Date.now() < end) {
                const el = document.querySelector(selector);
                if (el) return el;
                await waitMs(120);
            }
            return null;
        };

        const position = (el) => {
            const rect = el.getBoundingClientRect();
            const pad = 6;
            spotlight.style.top = `${Math.max(0, rect.top - pad)}px`;
            spotlight.style.left = `${Math.max(0, rect.left - pad)}px`;
            spotlight.style.width = `${rect.width + pad * 2}px`;
            spotlight.style.height = `${rect.height + pad * 2}px`;

            const popRect = popover.getBoundingClientRect();
            let top = rect.bottom + 12;
            let left = rect.left;
            if (top + popRect.height > window.innerHeight - 8) {
                top = Math.max(8, rect.top - popRect.height - 12);
            }
            if (left + popRect.width > window.innerWidth - 8) {
                left = window.innerWidth - popRect.width - 8;
            }
            popover.style.top = `${top}px`;
            popover.style.left = `${Math.max(8, left)}px`;
        };

        const renderStep = async () => {
            if (!state.active) return;
            const step = steps[index];
            if (step.route && window.location.pathname !== step.route && dotNetBridge?.invokeMethodAsync) {
                await dotNetBridge.invokeMethodAsync('NavigateForTourAsync', step.route);
                await waitMs(900);
            }

            const el = await waitForElement(step.element, 12000);
            if (!el) {
                index++;
                if (index >= steps.length) {
                    await finish();
                    return;
                }
                await renderStep();
                return;
            }

            el.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            await waitMs(200);

            popover.innerHTML = `
                <p class="tikr-tour-step">${index + 1} of ${steps.length}</p>
                <h3 class="tikr-tour-title"></h3>
                <p class="tikr-tour-desc"></p>
                <div class="tikr-tour-actions">
                    <button type="button" class="tikr-tour-skip">Skip tour</button>
                    <button type="button" class="tikr-tour-next">${index + 1 >= steps.length ? 'Done' : 'Next'}</button>
                </div>`;
            popover.querySelector('.tikr-tour-title').textContent = step.title;
            popover.querySelector('.tikr-tour-desc').textContent = step.description;
            popover.querySelector('.tikr-tour-skip').onclick = () => finish();
            popover.querySelector('.tikr-tour-next').onclick = async () => {
                index++;
                if (index >= steps.length) await finish();
                else await renderStep();
            };

            position(el);
            const onResize = () => position(el);
            window.addEventListener('resize', onResize);
            popover._cleanup = () => window.removeEventListener('resize', onResize);
        };

        overlay.onclick = () => finish();
        const onKey = (e) => {
            if (e.key === 'Escape') finish();
        };
        document.addEventListener('keydown', onKey);

        tikrTour._activeCleanup = () => {
            document.removeEventListener('keydown', onKey);
            if (popover._cleanup) popover._cleanup();
        };

        await renderStep();
    },

    _destroy() {
        if (tikrTour._activeCleanup) {
            tikrTour._activeCleanup();
            tikrTour._activeCleanup = null;
        }
        document.querySelectorAll('.tikr-tour-overlay, .tikr-tour-spotlight, .tikr-tour-popover').forEach((n) => n.remove());
    }
};