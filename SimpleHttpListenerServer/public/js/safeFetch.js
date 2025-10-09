import { logout } from './session.js';
import { renderAuthButtons } from './auth.js';
import { createGuestBoard } from './api.js'; // можно вызвать повторно при выходе

export async function safeFetch(url, options = {}) {
    const res = await fetch(url, options);

    // если токен истёк или невалиден
    if (res.status === 401) {
        console.warn("🔒 Token expired. Switching to guest mode...");
        logout();
        renderAuthButtons();

        // создаём гостевую доску, чтобы не было пустоты
        await createGuestBoard();
        throw new Error("Unauthorized: user logged out automatically");
    }

    // если другой тип ошибки
    if (!res.ok) {
        const text = await res.text();
        throw new Error(`HTTP ${res.status}: ${text}`);
    }

    return res;
}