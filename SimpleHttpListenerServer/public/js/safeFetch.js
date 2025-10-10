import { logout } from './session.js';
import { renderAuthButtons } from './auth.js';
import { createGuestBoard } from './api.js'; // можно вызвать повторно при выходе
import { showToast } from './toast.js';

export async function safeFetch(url, options = {}) {
    const res = await fetch(url, options);

    if (res.status === 401) {
        console.warn("🔒 Token expired. Switching to guest mode...");
        logout();
        renderAuthButtons();
        await createGuestBoard();
        showToast("Сессия истекла — вы вошли как гость", "warning");
        throw new Error("Unauthorized: user logged out automatically");
    }

    if (!res.ok) {
        const text = await res.text();
        showToast(`Ошибка: ${res.status}`, "error");
        throw new Error(`HTTP ${res.status}: ${text}`);
    }

    return res;
}