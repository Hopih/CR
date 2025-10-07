export function createForm(title, fields, onSubmit) {
    const existingOverlay = document.querySelector('.form-overlay');
    if (existingOverlay) existingOverlay.remove();

    // создаём фон-оверлей
    const overlay = document.createElement('div');
    overlay.className = 'form-overlay show'; // класс show — для анимации появления
    
    const formContainer = document.createElement('div');
    formContainer.className = 'form-container';

    const formTitle = document.createElement('h2');
    formTitle.textContent = title;
    formContainer.appendChild(formTitle);

    const form = document.createElement('form');
    fields.forEach((field, i) => {
        const input = document.createElement('input');
        input.type = field.type;
        input.placeholder = field.placeholder;
        input.name = field.name || `field${i}`;
        input.required = true;
        form.appendChild(input);
    });

    const button = document.createElement('button');
    button.type = 'button';
    button.textContent = title;
    button.addEventListener('click', async () => {
        let user = {};
        const formData = new FormData(form);
        formData.forEach((v, k) => (user[k] = v));
        await onSubmit(user);
        closeOverlay(); // при успешной отправке плавно закрываем
    });

    form.appendChild(button);
    formContainer.appendChild(form);
    overlay.appendChild(formContainer);
    document.body.appendChild(overlay);

    // === функция плавного закрытия ===
    function closeOverlay() {
        overlay.classList.remove('show');
        overlay.classList.add('hide');
        overlay.addEventListener('animationend', () => overlay.remove(), { once: true });
        document.removeEventListener('keydown', escHandler);
    }

    // клик по фону
    overlay.addEventListener('click', (e) => {
        if (e.target === overlay) closeOverlay();
    });

    // Esc
    function escHandler(e) {
        if (e.key === 'Escape') closeOverlay();
    }
    document.addEventListener('keydown', escHandler);
}

