import { createForm } from './ui.js';
import {signup, login} from './api.js';

export function initAuth() {
    document.getElementById('signup-link').addEventListener('click', (e) => {
        e.preventDefault();
        createForm('Register', [
            {type: 'text', placeholder: 'Username', name: 'login'},
            {type: 'password', placeholder: 'Password', name: 'password'},
        ], signup);
    });

    document.getElementById('signin-link').addEventListener('click', (e) => {
        e.preventDefault();
        createForm('Login', [
            {type: 'text', placeholder: 'Username', name: 'login'},
            {type: 'password', placeholder: 'Password', name: 'password'},
        ], login);
    });
}