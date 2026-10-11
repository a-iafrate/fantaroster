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

// Blazor's enhanced form handling answers without a page load and patches the DOM: let the form be submitted again.
document.addEventListener('DOMContentLoaded', () => {
    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', () => {
            document.querySelectorAll('form[data-submitted]').forEach((form) => delete form.dataset.submitted);
        });
    }
});
