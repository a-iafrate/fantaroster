let dotNetRef;
let onlineHandler;
let offlineHandler;

export function initialize(ref) {
    dotNetRef = ref;
    
    onlineHandler = () => dotNetRef.invokeMethodAsync('UpdateStatus', true);
    offlineHandler = () => dotNetRef.invokeMethodAsync('UpdateStatus', false);

    window.addEventListener('online', onlineHandler);
    window.addEventListener('offline', offlineHandler);

    return navigator.onLine;
}

export function dispose() {
    if (onlineHandler) {
        window.removeEventListener('online', onlineHandler);
    }
    if (offlineHandler) {
        window.removeEventListener('offline', offlineHandler);
    }
}
