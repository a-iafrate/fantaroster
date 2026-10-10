// Theme preference: "system", "light" or "dark", stored per device.
// Loaded synchronously in <head> so the resolved theme is applied before first paint.
//
// Surfaces: a layout can mark its root with [data-theme-surface="name"].
//  - data-theme-default="dark" changes the default preference for that surface
//    (the referee console starts dark); the choice is stored per surface.
//  - data-theme-lock="stage" pins the theme and ignores the preference (big screen).
//
// Any element with [data-theme-cycle] cycles the preference; [data-theme-label] inside it
// shows the localized label taken from data-label-{preference}.
(function () {
    'use strict';

    var storagePrefix = 'roster-theme';
    var order = ['system', 'light', 'dark'];
    var icons = { system: 'icon-monitor', light: 'icon-sun', dark: 'icon-moon' };
    var media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    var root = document.documentElement;

    function currentSurface() {
        return document.querySelector('[data-theme-surface]');
    }

    function storageKey(surface) {
        var name = surface ? surface.getAttribute('data-theme-surface') : '';
        return surface && surface.hasAttribute('data-theme-default') ? storagePrefix + '.' + name : storagePrefix;
    }

    function readPreference(surface) {
        var fallback = surface && surface.getAttribute('data-theme-default');
        if (order.indexOf(fallback) < 0) {
            fallback = 'system';
        }
        try {
            var value = localStorage.getItem(storageKey(surface));
            return order.indexOf(value) >= 0 ? value : fallback;
        } catch (e) {
            return fallback;
        }
    }

    function writePreference(surface, value) {
        try {
            localStorage.setItem(storageKey(surface), value);
        } catch (e) {
            // Storage unavailable (private mode): the choice lasts until the next page load.
        }
    }

    function resolve(surface, preference) {
        var lock = surface && surface.getAttribute('data-theme-lock');
        if (lock) {
            return lock;
        }
        if (preference === 'system') {
            return media && media.matches ? 'dark' : 'light';
        }
        return preference;
    }

    function setAttribute(name, value) {
        if (root.getAttribute(name) !== value) {
            root.setAttribute(name, value);
        }
    }

    function applyTheme() {
        var surface = currentSurface();
        var preference = readPreference(surface);
        var theme = resolve(surface, preference);
        setAttribute('data-theme', theme);
        setAttribute('data-theme-preference', preference);
        // Bootstrap components (selects, close buttons) follow their own attribute.
        setAttribute('data-bs-theme', theme === 'dark' ? 'dark' : 'light');
        var scheme = theme === 'dark' ? 'dark' : 'light';
        if (root.style.colorScheme !== scheme) {
            root.style.colorScheme = scheme;
        }
        return preference;
    }

    function updateControls(preference) {
        var buttons = document.querySelectorAll('[data-theme-cycle]');
        for (var i = 0; i < buttons.length; i++) {
            var button = buttons[i];
            var label = button.getAttribute('data-label-' + preference) || '';
            var labelTarget = button.querySelector('[data-theme-label]');
            if (labelTarget && labelTarget.textContent !== label) {
                labelTarget.textContent = label;
            }
            var template = button.getAttribute('data-aria-template');
            if (template) {
                var aria = template.replace('{0}', label);
                if (button.getAttribute('aria-label') !== aria) {
                    button.setAttribute('aria-label', aria);
                }
            }
            var icon = button.querySelector('[data-theme-icon]');
            if (icon && icon.className !== icons[preference]) {
                icon.className = icons[preference];
            }
        }
    }

    function sync() {
        updateControls(applyTheme());
    }

    applyTheme();

    document.addEventListener('click', function (event) {
        var target = event.target instanceof Element ? event.target.closest('[data-theme-cycle]') : null;
        if (!target) {
            return;
        }
        event.preventDefault();
        var surface = currentSurface();
        var next = order[(order.indexOf(readPreference(surface)) + 1) % order.length];
        writePreference(surface, next);
        sync();
    });

    if (media) {
        if (media.addEventListener) {
            media.addEventListener('change', sync);
        } else if (media.addListener) {
            media.addListener(sync);
        }
    }

    // Another tab changed a preference.
    window.addEventListener('storage', function (event) {
        if (event.key && event.key.indexOf(storagePrefix) === 0) {
            sync();
        }
    });

    // Blazor enhanced navigation merges the <html> attributes from the server response,
    // which drops the theme attributes: restore them as soon as they change.
    new MutationObserver(applyTheme).observe(root, {
        attributes: true,
        attributeFilter: ['data-theme', 'data-theme-preference', 'data-bs-theme']
    });

    // While the page is parsed, apply the surface theme as soon as its root appears,
    // so a dark-by-default surface does not flash light. Afterwards, keep the theme and
    // the toggle labels in sync with enhanced navigation and interactive renders.
    var parsing = new MutationObserver(function () {
        if (currentSurface()) {
            applyTheme();
            parsing.disconnect();
        }
    });
    parsing.observe(root, { childList: true, subtree: true });

    // Live pages re-render often: coalesce bursts of mutations into one sync before the next paint.
    var syncQueued = false;
    function queueSync() {
        if (syncQueued) {
            return;
        }
        syncQueued = true;
        Promise.resolve().then(function () {
            syncQueued = false;
            sync();
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        parsing.disconnect();
        sync();
        new MutationObserver(queueSync).observe(document.body, { childList: true, subtree: true });
    });
})();
