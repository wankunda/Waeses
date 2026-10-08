// UI Library JS Interop - Focus Trap & Keyboard handlers
window.UiInterop = {

    applyGlobalTheme: function (theme, density, palette) {
        const root = document.documentElement;
        if (!root._uiThemePrevious) {
            root._uiThemePrevious = {
                theme: root.getAttribute('data-theme'),
                density: root.getAttribute('data-ui-density'),
                style: root.getAttribute('style')
            };
        }

        if (root._uiThemeSystemListener) {
            window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', root._uiThemeSystemListener);
            delete root._uiThemeSystemListener;
        }

        if (theme === 'light' || theme === 'dark') {
            root.setAttribute('data-theme', theme);
            root.style.colorScheme = theme;
        } else {
            const media = window.matchMedia('(prefers-color-scheme: dark)');
            const applySystemTheme = function (event) {
                const systemTheme = event.matches ? 'dark' : 'light';
                root.setAttribute('data-theme', systemTheme);
                root.style.colorScheme = systemTheme;
            };
            applySystemTheme(media);
            media.addEventListener('change', applySystemTheme);
            root._uiThemeSystemListener = applySystemTheme;
        }

        root.setAttribute('data-ui-density', density);
        Object.entries(palette || {}).forEach(function (entry) {
            root.style.setProperty('--' + entry[0], entry[1]);
        });
    },

    clearGlobalTheme: function () {
        const root = document.documentElement;
        const previous = root._uiThemePrevious;
        if (!previous) return;

        if (root._uiThemeSystemListener) {
            window.matchMedia('(prefers-color-scheme: dark)').removeEventListener('change', root._uiThemeSystemListener);
            delete root._uiThemeSystemListener;
        }

        if (previous.theme === null) root.removeAttribute('data-theme');
        else root.setAttribute('data-theme', previous.theme);

        if (previous.density === null) root.removeAttribute('data-ui-density');
        else root.setAttribute('data-ui-density', previous.density);

        if (previous.style === null) root.removeAttribute('style');
        else root.setAttribute('style', previous.style);

        delete root._uiThemePrevious;
    },

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

    setupOverlay: function (rootEl, dotNetRef) {
        if (!rootEl) return;

        rootEl._uiHandleOverlayOutsideClick = function (event) {
            if (!rootEl.contains(event.target)) {
                dotNetRef.invokeMethodAsync('CloseOverlay');
            }
        };
        rootEl._uiHandleOverlayKeydown = function (event) {
            if (event.key === 'Escape') {
                dotNetRef.invokeMethodAsync('CloseOverlay');
            }
        };
        document.addEventListener('mousedown', rootEl._uiHandleOverlayOutsideClick);
        document.addEventListener('keydown', rootEl._uiHandleOverlayKeydown);
    },

    setupMenu: function (rootEl, dotNetRef) {
        if (!rootEl) return;

        rootEl._uiMenuDotNetRef = dotNetRef;
        rootEl._uiMenuHandleOutsideClick = function (event) {
            const menu = rootEl.querySelector('[role="menu"]');
            if (!rootEl.contains(event.target) && menu && menu.matches(':popover-open')) {
                rootEl._uiMenuDotNetRef.invokeMethodAsync('CloseMenuFromOutside');
            }
        };
        rootEl._uiMenuHandleKeydown = function (event) {
            const trigger = rootEl.querySelector('.ui-menu-trigger');
            const menu = rootEl.querySelector('[role="menu"]');
            const isOpen = menu && menu.matches(':popover-open');

            if (event.key === 'Escape' && isOpen && rootEl.contains(document.activeElement)) {
                event.preventDefault();
                rootEl._uiMenuDotNetRef.invokeMethodAsync('CloseMenuFromKeyboard');
                return;
            }

            if (event.target === trigger && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
                event.preventDefault();
                rootEl._uiMenuDotNetRef.invokeMethodAsync('OpenMenuFromKeyboard');
                return;
            }

            if (!menu || !menu.contains(event.target)) return;
            if (event.key === 'Tab') {
                rootEl._uiMenuDotNetRef.invokeMethodAsync('CloseMenuFromOutside');
                return;
            }

            const items = Array.from(menu.querySelectorAll(
                '[role="menuitem"], button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'
            )).filter(function (item) {
                return item.getClientRects().length > 0 &&
                    item.getAttribute('aria-disabled') !== 'true' &&
                    !item.hasAttribute('disabled');
            });
            if (items.length === 0) return;

            const currentIndex = items.indexOf(document.activeElement);
            let nextIndex = currentIndex;
            if (event.key === 'ArrowDown') nextIndex = (currentIndex + 1) % items.length;
            else if (event.key === 'ArrowUp') nextIndex = (currentIndex - 1 + items.length) % items.length;
            else if (event.key === 'Home') nextIndex = 0;
            else if (event.key === 'End') nextIndex = items.length - 1;
            else return;

            event.preventDefault();
            items[nextIndex].focus();
        };

        document.addEventListener('pointerdown', rootEl._uiMenuHandleOutsideClick, true);
        document.addEventListener('keydown', rootEl._uiMenuHandleKeydown, true);
    },

    showMenu: function (rootEl, trigger, menu, alignStart) {
        if (!rootEl || !trigger || !menu) return;

        if (!menu.matches(':popover-open')) {
            menu.showPopover();
        }

        rootEl._uiMenuPosition = function () {
            if (!menu.isConnected || !menu.matches(':popover-open')) return;

            const triggerRect = trigger.getBoundingClientRect();
            const menuRect = menu.getBoundingClientRect();
            const viewportPadding = 8;
            const gap = 4;
            const preferredLeft = alignStart
                ? triggerRect.left
                : triggerRect.right - menuRect.width;
            const left = Math.max(
                viewportPadding,
                Math.min(preferredLeft, window.innerWidth - menuRect.width - viewportPadding)
            );
            const top = triggerRect.bottom + gap + menuRect.height <= window.innerHeight - viewportPadding
                ? triggerRect.bottom + gap
                : Math.max(viewportPadding, triggerRect.top - menuRect.height - gap);

            menu.style.left = `${left}px`;
            menu.style.top = `${top}px`;
        };

        if (!rootEl._uiMenuPositionHandler) {
            rootEl._uiMenuPositionHandler = function () {
                if (rootEl._uiMenuPosition) rootEl._uiMenuPosition();
            };
            window.addEventListener('scroll', rootEl._uiMenuPositionHandler, true);
            window.addEventListener('resize', rootEl._uiMenuPositionHandler);
        }

        rootEl._uiMenuPosition();
        requestAnimationFrame(rootEl._uiMenuPosition);
    },

    hideMenu: function (rootEl) {
        if (!rootEl || !rootEl._uiMenuPositionHandler) return;
        window.removeEventListener('scroll', rootEl._uiMenuPositionHandler, true);
        window.removeEventListener('resize', rootEl._uiMenuPositionHandler);
        delete rootEl._uiMenuPositionHandler;
        delete rootEl._uiMenuPosition;
    },

    teardownMenu: function (rootEl) {
        if (!rootEl) return;
        window.UiInterop.hideMenu(rootEl);
        if (rootEl._uiMenuHandleOutsideClick) {
            document.removeEventListener('pointerdown', rootEl._uiMenuHandleOutsideClick, true);
            delete rootEl._uiMenuHandleOutsideClick;
        }
        if (rootEl._uiMenuHandleKeydown) {
            document.removeEventListener('keydown', rootEl._uiMenuHandleKeydown, true);
            delete rootEl._uiMenuHandleKeydown;
        }
        delete rootEl._uiMenuDotNetRef;
    },

    focusMenuItem: function (menuEl, first) {
        if (!menuEl) return;
        const items = Array.from(menuEl.querySelectorAll(
            '[role="menuitem"], button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])'
        )).filter(function (item) {
            return item.getClientRects().length > 0 &&
                item.getAttribute('aria-disabled') !== 'true' &&
                !item.hasAttribute('disabled');
        });
        if (items.length > 0) items[first ? 0 : items.length - 1].focus();
    },

    focusElement: function (element) {
        if (element && typeof element.focus === 'function') element.focus();
    },

    teardownOverlay: function (rootEl) {
        if (!rootEl) return;
        if (rootEl._uiHandleOverlayOutsideClick) {
            document.removeEventListener('mousedown', rootEl._uiHandleOverlayOutsideClick);
            delete rootEl._uiHandleOverlayOutsideClick;
        }
        if (rootEl._uiHandleOverlayKeydown) {
            document.removeEventListener('keydown', rootEl._uiHandleOverlayKeydown);
            delete rootEl._uiHandleOverlayKeydown;
        }
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
    },

    copyText: async function (text) {
        await navigator.clipboard.writeText(text || '');
    }
};
