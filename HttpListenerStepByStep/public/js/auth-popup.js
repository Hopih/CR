export function createAuthPopup(dialog) {
    const form = dialog.querySelector('form');
    const title = dialog.querySelector('h3');
    const description = dialog.querySelector('.popup-description');
    const error = dialog.querySelector('.popup-error');
    const cancel = dialog.querySelector('.popup-cancel');
    const submit = dialog.querySelector('.popup-submit');
    let options = null;
    let busy = false;
    
    function clearError() {
        error.hidden = true; error.textContent = '';
        form.querySelectorAll('[aria-invalid]').forEach(x => x.removeAttribute('aria-invalid'));
    }
    
    function close() { 
        if (!busy) 
            dialog.close(); 
    }
    cancel.addEventListener('click', close);
    dialog.addEventListener('cancel', event => { if (busy) event.preventDefault(); });
    dialog.addEventListener('close', () => { options = null; form.reset(); });
    dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right ||
            event.clientY < rect.top || event.clientY > rect.bottom) close();
    });
    form.addEventListener('input', clearError);
    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (!options || busy) return;
        const value = {
            login: form.elements.login.value.trim().toLowerCase(),
            password: form.elements.password.value
        };
        
        const register = options.mode === 'register';
        if (register) 
            value.displayName = form.elements.displayName.value.trim();
        
        let message = '';
        
        if (!/^[a-z0-9_]{3,40}$/.test(value.login))
            message = 'Логин: 3-40 символов a-z, 0-9, _';
        else if (register && (!value.displayName || value.displayName.length > 80))
            message = 'Имя: от 1 до 80 символов';
        else if (value.password.length < (register ? 12 : 1) || value.password.length > 128)
            message = register ? 'Пароль: 12-128 символов' : 'Введите пароль до 128 символов';
        if (message) { 
            error.textContent = message; error.hidden = false; return; 
        }
        busy = true; submit.disabled = true; cancel.disabled = true;
        try {
            await options.send(value);
            dialog.close();
        } 
        catch (ex) { 
            error.textContent = ex.message; error.hidden = false; 
        }
        finally {
            busy = false; submit.disabled = false; cancel.disabled = false; 
        }
    });
        return function open(value) {
            if (dialog.open) return;
            
            options = value; form.reset(); clearError();
            const register = value.mode === 'register';
            title.textContent = register ? 'Регистрация' : 'Вход';
            
            description.textContent = register 
                ? 'Создайте аккаунт. Вход выполняется отдельно.'
                : 'Войдите, чтобы открыть свои доски.';
            
            form.elements.displayName.closest('.popup-field').hidden = !register;
            form.elements.password.autocomplete = register ? 'new-password' : 'current-password';
            submit.textContent = register ? 'Создать аккаунт' : 'Войти';
            dialog.showModal(); form.elements.login.focus();
        };
    }