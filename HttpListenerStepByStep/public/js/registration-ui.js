import { createAuthPopup } from './auth-popup.js';
import { register } from './auth-api.js';
export function initializeRegistrationUI() {
    const open = createAuthPopup(document.getElementById('authPopup'));
    document.getElementById('registerButton').addEventListener('click', () => open({
        mode: 'register',
        send: async value => {
            const user = await register(value);
            document.getElementById('boardStatus').textContent =
                `Создан @${user.login}, UUID: ${user.id}. Вход добавим следующим шагом.`;
        }
    }));
}