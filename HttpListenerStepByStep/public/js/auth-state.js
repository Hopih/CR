let current = null;
let deadlineTimer;
export const getSession = () => current;
export function setSession(value) {
    if (current === null && value === null){
        return;
    } 
    clearTimeout(deadlineTimer);
    
    current = value;
    
    if (value) {
        const delay = Date.parse(value.absoluteExpiresAt) - Date.now();
        // При рассинхронизации часов не создаём цикл немедленных перезагрузок.
        // Сервер всё равно проверяет срок при каждом запросе.
        if (delay > 0) deadlineTimer = setTimeout(() => setSession(null), delay);
    }
    window.dispatchEvent(new CustomEvent('session-changed', { detail: current }));
}