import * as api from './auth-api.js';
import { ApiError } from './http-client.js';
import { getSession, setSession } from './auth-state.js';
import { createAuthPopup } from './auth-popup.js';
export async function initializeUserHeader() {
    const info = document.getElementById('userInfo');
    const enter = document.getElementById('loginButton');
    const leave = document.getElementById('logoutButton');
    const registerButton = document.getElementById('registerButton');
    enter.disabled = true;
    leave.disabled = true;
    registerButton.disabled = true;
    const status = document.getElementById('boardStatus');
    const open = createAuthPopup(document.getElementById('authPopup'));

    function render() {
        const value = getSession();
        info.textContent = value
            ? `${value.user.displayName} (@${value.user.login}) · ${value.user.role}` : 'Гость';
        enter.hidden = Boolean(value);
        leave.hidden = !value;
    }

    window.addEventListener('session-changed', render);
    registerButton.addEventListener('click', () => open({
        mode: 'register', send: async value => {
            const user = await api.register(value);
            status.textContent = `Аккаунт @${user.login} создан. Теперь войдите.`;
        }
    }));
    enter.addEventListener('click', () => open({
        mode: 'login', send: async value => setSession(await api.login(value))
    }));
    leave.addEventListener('click', async () => {
        leave.disabled = true;
        try {
            await api.logout();
            setSession(null);
        } catch (ex) {
            status.textContent = `Не удалось выйти: ${ex.message}`;
        } finally {
            leave.disabled = false;
        }
    });
    info.textContent = 'Проверяем сессию...';
    try {
        setSession(await api.me());
    } catch (ex) {
        if (!(ex instanceof ApiError && ex.status === 401)) {
            info.textContent = 'Не удалось проверить сессию';
            enter.disabled = true;
            throw ex;
        }
    } finally {
        leave.disabled = false;
        registerButton.disabled = false;
    }
    enter.disabled = false;
    render();
    return getSession();
}