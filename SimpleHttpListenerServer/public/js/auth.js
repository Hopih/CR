import { createForm } from './ui.js';

export function initAuth() {
    document.getElementById('signup-link').addEventListener('click', (e) => {
        e.preventDefault();
        createForm('Register', [
            { type: 'text', placeholder: 'Username', name: 'login' },
            { type: 'password', placeholder: 'Password', name: 'password' },
        ], signup);
    });

    document.getElementById('signin-link').addEventListener('click', (e) => {
        e.preventDefault();
        createForm('Login', [
            { type: 'text', placeholder: 'Username', name: 'login' },
            { type: 'password', placeholder: 'Password', name: 'password' },
        ], login);
    });
}

async function signup(user) {
    let result = await fetch('/user/signup', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(user)
    });
    if (result.ok) {
        console.log("Регистрация успешна");
    }
}

async function login(user) {
    let result = await fetch('/user/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(user)
    });
    if (result.ok) {
        const data = await result.json();
        localStorage.setItem('token', data.token);
        console.log("Авторизация успешна");
    }
}