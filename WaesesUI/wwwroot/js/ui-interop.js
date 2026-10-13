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

    // ---- Thème (sans eval : compatible avec une politique CSP stricte) ----
    getStoredTheme: function () {
        try { return window.localStorage.getItem('app_theme'); } catch (e) { return null; }
    },

    prefersDark: function () {
        return !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
    },

    setTheme: function (name) {
        const root = document.documentElement;
        root.setAttribute('data-theme', name);
        root.style.colorScheme = name;
        try { window.localStorage.setItem('app_theme', name); } catch (e) { /* stockage indisponible */ }
    },

    // ---- Calendrier ----
    focusInside: function (container, selector) {
        if (!container) return;
        const element = container.querySelector(selector);
        if (element && typeof element.focus === 'function') element.focus();
    },

    // Les flèches, Début, Fin et Page précédente/suivante déplacent la sélection de jour :
    // sans cela, le navigateur ferait aussi défiler la page.
    setupCalendar: function (rootEl) {
        if (!rootEl || rootEl._uiCalendarKeydown) return;
        const keys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];
        rootEl._uiCalendarKeydown = function (event) {
            if (keys.indexOf(event.key) >= 0 && event.target.closest && event.target.closest('.ui-cal-grid')) {
                event.preventDefault();
            }
        };
        rootEl.addEventListener('keydown', rootEl._uiCalendarKeydown);
    },

    teardownCalendar: function (rootEl) {
        if (!rootEl || !rootEl._uiCalendarKeydown) return;
        rootEl.removeEventListener('keydown', rootEl._uiCalendarKeydown);
        delete rootEl._uiCalendarKeydown;
    },

    focusById: function (id) {
        const element = document.getElementById(id);
        if (element && typeof element.focus === 'function') element.focus();
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
    // options.defaultFocus : 'firstChild' (défaut), 'lastChild', 'element' (la boîte role="dialog") ou 'none'.
    setupModal: function (modalEl, dotNetRef, options) {
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
        modalEl._uiPreviousFocus = document.activeElement;
        window.UiInterop._modalStack.push(modalEl);

        // Focus the first focusable element (or the modal box itself)
        const defaultFocus = (options && options.defaultFocus) || 'firstChild';
        if (defaultFocus === 'none') return;
        requestAnimationFrame(function () {
            const box = modalEl.querySelector('[role="dialog"]') || modalEl;
            const focusable = window.UiInterop._getFocusableElements(modalEl);
            if (defaultFocus === 'element' || focusable.length === 0) {
                box.focus();
            } else if (defaultFocus === 'lastChild') {
                focusable[focusable.length - 1].focus();
            } else {
                focusable[0].focus();
            }
        });
    },

    // Modales ouvertes, de la plus ancienne à la plus récente.
    _modalStack: [],

    // Tear down focus trap
    teardownModal: function (modalEl) {
        const stack = window.UiInterop._modalStack;

        // Quand le parent ferme la modale (IsOpen = false), son élément est déjà retiré du DOM et la référence reçue est vide :
        // on retrouve alors la modale détachée la plus récente pour rendre le focus au déclencheur.
        let target = modalEl && modalEl._uiHandleKeyDown ? modalEl : null;
        if (!target) {
            for (let i = stack.length - 1; i >= 0; i--) {
                if (!stack[i].isConnected) { target = stack[i]; break; }
            }
        }
        if (!target) return;

        const index = stack.indexOf(target);
        if (index >= 0) stack.splice(index, 1);

        if (target._uiHandleKeyDown) {
            target.removeEventListener('keydown', target._uiHandleKeyDown);
            delete target._uiHandleKeyDown;
        }

        // Rend le focus à l'élément qui a ouvert la modale
        const previous = target._uiPreviousFocus;
        delete target._uiPreviousFocus;
        if (previous && previous.isConnected && typeof previous.focus === 'function') {
            previous.focus();
        }
    },

    // Lock body scroll when modal is open
    // Compteur : plusieurs modales empilées ne déverrouillent la page qu'à la dernière fermeture
    _scrollLocks: 0,

    lockBodyScroll: function () {
        window.UiInterop._scrollLocks++;
        document.body.style.overflow = 'hidden';
    },

    unlockBodyScroll: function () {
        window.UiInterop._scrollLocks = Math.max(0, window.UiInterop._scrollLocks - 1);
        if (window.UiInterop._scrollLocks === 0) {
            document.body.style.overflow = '';
        }
    },

    // ---------- Popovers (rendus par UiPopoverProvider) ----------
    _popovers: new Map(),

    _resolvePopoverAnchor: function (anchorId, options) {
        if (options && options.anchorSelector) {
            const found = document.querySelector(options.anchorSelector);
            if (found) return found;
        }
        const placeholder = document.getElementById(anchorId);
        return placeholder ? placeholder.parentElement : null;
    },

    openPopover: function (layerId, anchorId, options, dotNetRef) {
        const layer = document.getElementById(layerId);
        const anchor = window.UiInterop._resolvePopoverAnchor(anchorId, options);
        if (!layer || !anchor) return;

        window.UiInterop.closePopover(layerId);

        const state = { layer, anchorId, anchor, options, dotNetRef };
        const close = function () { dotNetRef.invokeMethodAsync('RequestClose'); };

        state.onPointerDown = function (event) {
            if (!state.options.closeOnOutsideClick) return;
            if (layer.contains(event.target) || state.anchor.contains(event.target)) return;
            close();
        };
        state.onKeyDown = function (event) {
            if (event.key === 'Escape' && state.options.closeOnEscape) {
                event.stopPropagation();
                close();
            }
        };
        state.onReposition = function () { window.UiInterop._placePopover(state); };

        document.addEventListener('mousedown', state.onPointerDown);
        document.addEventListener('keydown', state.onKeyDown);
        window.addEventListener('resize', state.onReposition);
        window.addEventListener('scroll', state.onReposition, true);

        window.UiInterop._popovers.set(layerId, state);
        window.UiInterop._placePopover(state);
    },

    positionPopover: function (layerId, anchorId, options) {
        const state = window.UiInterop._popovers.get(layerId);
        if (!state) return;
        state.options = options;
        state.anchor = window.UiInterop._resolvePopoverAnchor(anchorId, options) || state.anchor;
        window.UiInterop._placePopover(state);
    },

    closePopover: function (layerId) {
        const state = window.UiInterop._popovers.get(layerId);
        if (!state) return;
        document.removeEventListener('mousedown', state.onPointerDown);
        document.removeEventListener('keydown', state.onKeyDown);
        window.removeEventListener('resize', state.onReposition);
        window.removeEventListener('scroll', state.onReposition, true);
        window.UiInterop._popovers.delete(layerId);
    },

    _placePopover: function (state) {
        const { layer, anchor, options } = state;
        if (!layer.isConnected || !anchor.isConnected) return;

        const margin = 8;
        const gap = options.offset || 0;
        const a = anchor.getBoundingClientRect();

        layer.style.minWidth = options.matchWidth ? a.width + 'px' : '';
        const p = layer.getBoundingClientRect();
        const vw = document.documentElement.clientWidth;
        const vh = document.documentElement.clientHeight;

        let side = 'bottom', align = 'start';
        switch (options.placement) {
            case 'Bottom': side = 'bottom'; align = 'center'; break;
            case 'BottomEnd': side = 'bottom'; align = 'end'; break;
            case 'TopStart': side = 'top'; align = 'start'; break;
            case 'Top': side = 'top'; align = 'center'; break;
            case 'TopEnd': side = 'top'; align = 'end'; break;
            case 'Left': side = 'left'; align = 'center'; break;
            case 'Right': side = 'right'; align = 'center'; break;
        }

        // Retourne le côté s'il manque de place d'un côté et qu'il y en a plus de l'autre.
        if (side === 'bottom' && a.bottom + gap + p.height > vh - margin && a.top - gap - p.height >= margin) side = 'top';
        else if (side === 'top' && a.top - gap - p.height < margin && a.bottom + gap + p.height <= vh - margin) side = 'bottom';
        else if (side === 'right' && a.right + gap + p.width > vw - margin && a.left - gap - p.width >= margin) side = 'left';
        else if (side === 'left' && a.left - gap - p.width < margin && a.right + gap + p.width <= vw - margin) side = 'right';

        let top, left;
        if (side === 'bottom' || side === 'top') {
            top = side === 'bottom' ? a.bottom + gap : a.top - gap - p.height;
            left = align === 'start' ? a.left : align === 'end' ? a.right - p.width : a.left + (a.width - p.width) / 2;
        } else {
            left = side === 'right' ? a.right + gap : a.left - gap - p.width;
            top = a.top + (a.height - p.height) / 2;
        }

        left = Math.min(Math.max(left, margin), Math.max(margin, vw - p.width - margin));
        top = Math.min(Math.max(top, margin), Math.max(margin, vh - p.height - margin));

        layer.style.transform = 'translate(' + Math.round(left) + 'px,' + Math.round(top) + 'px)';
        layer.dataset.side = side;
        layer.dataset.positioned = 'true';
    },

    copyText: async function (text) {
        await navigator.clipboard.writeText(text || '');
    }
};
