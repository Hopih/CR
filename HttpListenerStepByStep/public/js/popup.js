// Один небольшой диалог для ввода значения и подтверждения действия.
export function createPopup(dialog) {
    const form = dialog.querySelector('form');
    const title = dialog.querySelector('h3');
    const description = dialog.querySelector('.popup-description');
    const field = dialog.querySelector('.popup-field');
    const label = dialog.querySelector('label');
    const input = dialog.querySelector('input');
    const error = dialog.querySelector('.popup-error');
    const cancel = dialog.querySelector('.popup-cancel');
    const submit = dialog.querySelector('.popup-submit');
    let current = null;
    let result = null;

    function clearError() {
        error.textContent = '';
        error.hidden = true;
        input.removeAttribute('aria-invalid');
    }

    input.addEventListener('input', clearError);
    cancel.addEventListener('click', () => dialog.close());
    form.addEventListener('submit', event => {
        event.preventDefault();
        if (!current) return;
        const value = input.value.trim();
        const message = current.confirm ? '' : current.validate(value);
        if (message) {
            error.textContent = message;
            error.hidden = false;
            input.setAttribute('aria-invalid', 'true');
            input.focus();
            return;
        }
        result = current.confirm ? true : value;
        dialog.close();
    });
    dialog.addEventListener('close', () => {
        const resolve = current?.resolve;
        current = null;
        resolve?.(result);
        result = null;
    });
    dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right ||
            event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
    });

    return function open(options) {
        if (current) return Promise.resolve(null);
        title.textContent = options.title;
        description.textContent = options.description ?? '';
        description.hidden = !description.textContent;
        field.hidden = Boolean(options.confirm);
        input.disabled = Boolean(options.confirm);
        label.textContent = options.label ?? '';
        input.value = options.value ?? '';
        input.maxLength = options.maxLength ?? 120;
        submit.textContent = options.accept ?? 'Сохранить';
        submit.classList.toggle('popup-danger', Boolean(options.danger));
        clearError();
        result = null;
        return new Promise(resolve => {
            current = { ...options, resolve };
            dialog.showModal();
            if (options.confirm) cancel.focus();
            else { input.focus(); input.select(); }
        });
    };
}
