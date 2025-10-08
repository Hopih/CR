import { logout, isAuthenticated } from './session.js';
//import { openBoards } from './board.js'; // функция перехода к списку досок (добавим позже)
import { createForm } from './ui.js';
import { login, signup } from './api.js';

export function renderAuthButtons() {
    const container = document.getElementById('auth-buttons');

    container.innerHTML = ''; // очищаем старые кнопки
    
    if (isAuthenticated()) {
        // 🔹 Пользователь вошёл
        const boardsBtn = document.createElement('button');
        boardsBtn.className = 'auth-button boards';
        boardsBtn.textContent = 'Boards';
        boardsBtn.addEventListener('click', openBoards);

        const logoutBtn = document.createElement('button');
        logoutBtn.className = 'auth-button logout';
        logoutBtn.textContent = 'Logout';
        logoutBtn.addEventListener('click', () => {
            logout();
            renderAuthButtons(); // перерисовать после выхода
        });

        container.append(boardsBtn, logoutBtn);
    } else {
        // 🔹 Гость
        const signinBtn = document.createElement('button');
        signinBtn.className = 'auth-button signin';
        signinBtn.textContent = 'Sign in';
        signinBtn.addEventListener('click', () => {
            createForm('Login', [
                { type: 'text', placeholder: 'Username', name: 'login' },
                { type: 'password', placeholder: 'Password', name: 'password' }
            ], async (user) => {
                await login(user.login, user.password);
                renderAuthButtons();
            });
        });

        const signupBtn = document.createElement('button');
        signupBtn.className = 'auth-button signup';
        signupBtn.textContent = 'Sign up';
        signupBtn.addEventListener('click', () => {
            createForm('Register', [
                { type: 'text', placeholder: 'Username', name: 'login' },
                { type: 'password', placeholder: 'Password', name: 'password' }
            ], async (user) => {
                await signup(user.login, user.password);
                renderAuthButtons();
            });
        });

        container.append(signinBtn, signupBtn);
    }
}