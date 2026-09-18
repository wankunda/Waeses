// UI Library JS Interop - Focus Trap & Keyboard handlers
window.UiInterop = {

    setupDrawer: function (drawerEl, toggleEl, dotNetRef) {
        if (!drawerEl) return;

        drawerEl._uiHandleEscape = function (event) {
            if (event.key === 'Escape') {
                dotNetRef.invokeMethodAsync('CloseDrawerAsync');
            }
        };

        document.addEventListener('keydown', drawerEl._uiHandleEscape);
    },

    updateDrawer: function (drawerEl, toggleEl, closeEl, isOpen) {
        if (!drawerEl) return;

        const isMobile = window.matchMedia('(max-width: 640.98px)').matches;
        const wasOpen = drawerEl._uiDrawerOpen;
        drawerEl.inert = isMobile && !isOpen;
        drawerEl.setAttribute('aria-hidden', String(isMobile && !isOpen));
        drawerEl._uiDrawerOpen = isOpen;

        if (!isMobile || wasOpen === isOpen) return;

        if (isOpen) {
            drawerEl._uiPreviousFocus = document.activeElement;
            requestAnimationFrame(function () {
                const focusable = window.UiInterop._getFocusableElements(drawerEl);
                if (focusable.length > 0) focusable[0].focus();
            });
        } else if (toggleEl && drawerEl._uiPreviousFocus === toggleEl) {
            toggleEl.focus();
        } else if (toggleEl) {
            toggleEl.focus();
        }
    },

    teardownDrawer: function (drawerEl) {
        if (!drawerEl || !drawerEl._uiHandleEscape) return;
        document.removeEventListener('keydown', drawerEl._uiHandleEscape);
        delete drawerEl._uiHandleEscape;
    },

    setupAutocomplete: function (rootEl, dotNetRef) {
        if (!rootEl) return;

        rootEl._uiHandleOutsideClick = function (event) {
            if (!rootEl.contains(event.target)) {
                dotNetRef.invokeMethodAsync('CloseAutocompleteAsync');
            }
        };

        document.addEventListener('mousedown', rootEl._uiHandleOutsideClick);
    },

    teardownAutocomplete: function (rootEl) {
        if (!rootEl || !rootEl._uiHandleOutsideClick) return;
        document.removeEventListener('mousedown', rootEl._uiHandleOutsideClick);
        delete rootEl._uiHandleOutsideClick;
    },

    // Focus trap: returns first/last focusable inside container
    _getFocusableElements: function (container) {
        const selectors = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
        return container.querySelectorAll(selectors);
    },

    // Set up focus trap and Escape handler for a modal element
    setupModal: function (modalEl, dotNetRef) {
        if (!modalEl) return;

        modalEl._uiHandleKeyDown = function (e) {
            // Escape key
            if (e.key === 'Escape') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OnEscapePressed');
                return;
            }

            // Focus trap on Tab
            if (e.key === 'Tab') {
                const focusable = window.UiInterop._getFocusableElements(modalEl);
                if (focusable.length === 0) return;

                const first = focusable[0];
                const last = focusable[focusable.length - 1];

                if (e.shiftKey) {
                    if (document.activeElement === first) {
                        e.preventDefault();
                        last.focus();
                    }
                } else {
                    if (document.activeElement === last) {
                        e.preventDefault();
                        first.focus();
                    }
                }
            }
        };

        modalEl.addEventListener('keydown', modalEl._uiHandleKeyDown);

        // Focus the first focusable element (or the modal box itself)
        requestAnimationFrame(function () {
            const focusable = window.UiInterop._getFocusableElements(modalEl);
            if (focusable.length > 0) {
                focusable[0].focus();
            } else {
                modalEl.focus();
            }
        });
    },

    // Tear down focus trap
    teardownModal: function (modalEl) {
        if (!modalEl || !modalEl._uiHandleKeyDown) return;
        modalEl.removeEventListener('keydown', modalEl._uiHandleKeyDown);
        delete modalEl._uiHandleKeyDown;
    },

    // Lock body scroll when modal is open
    lockBodyScroll: function () {
        document.body.style.overflow = 'hidden';
    },

    unlockBodyScroll: function () {
        document.body.style.overflow = '';
    }
};
