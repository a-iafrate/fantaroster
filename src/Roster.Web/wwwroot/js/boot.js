// Startup code that used to be inline in App.razor, so the Content Security Policy needs no inline scripts.

// The WebAssembly client picks its culture from the language the server chose for the page.
window.rosterGetBrowserLanguage = () => document.documentElement.lang || navigator.language || 'en';

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js').catch(function (error) {
        console.warn('Service worker registration failed:', error);
    });
}
