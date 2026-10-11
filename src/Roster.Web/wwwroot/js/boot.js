// Startup code that used to be inline in App.razor, so the Content Security Policy needs no inline scripts.

// The WebAssembly client picks its culture from the language the server chose for the page.
window.rosterGetBrowserLanguage = () => document.documentElement.lang || navigator.language || 'en';

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js').catch(function (error) {
        console.warn('Service worker registration failed:', error);
    });
}

// Forms that post to the server as a full page request (sign in, account deletion, sign out) answer slowly on a bad
// connection: once submitted, the button is disabled, shows a spinner and, if it has one, a "working" label.
document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.method !== 'post') {
        return;
    }

    if (form.dataset.submitted) {
        event.preventDefault();
        return;
    }

    const button = event.submitter || form.querySelector('button[type=submit]');
    form.dataset.submitted = '1';
    if (!button) {
        return;
    }

    // After the browser has collected the form data, otherwise a disabled button would drop out of the request.
    setTimeout(() => {
        button.setAttribute('aria-busy', 'true');
        button.disabled = true;
        const label = button.dataset.busyLabel;
        const last = button.lastChild;
        if (label && last && last.nodeType === Node.TEXT_NODE) {
            last.textContent = label;
        }
    }, 0);
}, true);

// Coming back with the browser's back button must not show a stuck button.
window.addEventListener('pageshow', (event) => {
    if (!event.persisted) {
        return;
    }
    document.querySelectorAll('form[data-submitted]').forEach((form) => {
        delete form.dataset.submitted;
        form.querySelectorAll('button[aria-busy]').forEach((button) => {
            button.removeAttribute('aria-busy');
            button.disabled = false;
        });
    });
});

// Blazor starts here (autostart is off) so a failed start can be told to the person instead of leaving dead buttons: the
// interactive pages need a real-time connection, and when the server or the connection service refuses it (for instance a
// quota reached) nothing on them reacts. Static content and native menus keep working.
function showCircuitError(error) {
    console.error('Blazor could not start', error);
    const banner = document.getElementById('circuit-error');
    if (banner) {
        banner.hidden = false;
    }
}

// Blazor does not always reject Blazor.start() when the connection is refused: it reports the failure as an unhandled error.
const circuitFailure = /negotiation|start the connection|Circuit host not initialized/i;
window.addEventListener('unhandledrejection', (event) => {
    if (circuitFailure.test(String(event.reason?.message ?? event.reason))) {
        showCircuitError(event.reason);
    }
});
window.addEventListener('error', (event) => {
    if (circuitFailure.test(String(event.message))) {
        showCircuitError(event.error);
    }
});

document.addEventListener('DOMContentLoaded', () => {
    if (!window.Blazor || !Blazor.start) {
        return;
    }

    Blazor.start().then(() => {
        // Blazor's enhanced form handling answers without a page load and patches the DOM: let the form be submitted again.
        if (Blazor.addEventListener) {
            Blazor.addEventListener('enhancedload', () => {
                document.querySelectorAll('form[data-submitted]').forEach((form) => delete form.dataset.submitted);
            });
        }
    }).catch(showCircuitError);
});

document.addEventListener('click', (event) => {
    if (event.target.closest('[data-circuit-reload]')) {
        location.reload();
    }
});

// Menus are native disclosures (<details data-menu>): they open before Blazor is interactive. Close them on a click outside,
// on a link (or an element marked data-menu-close) inside, and on Escape.
document.addEventListener('click', (event) => {
    document.querySelectorAll('details[data-menu][open]').forEach((menu) => {
        const inside = menu.contains(event.target);
        const onSummary = event.target.closest('summary') !== null && inside;
        if (!inside || (!onSummary && event.target.closest('a, [data-menu-close]'))) {
            menu.open = false;
        }
    });
});

document.addEventListener('keydown', (event) => {
    if (event.key !== 'Escape') {
        return;
    }
    document.querySelectorAll('details[data-menu][open]').forEach((menu) => {
        menu.open = false;
        menu.querySelector('summary')?.focus();
    });
});
