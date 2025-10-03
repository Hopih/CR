export function createForm(title, fields, onSubmit) {
    const board = document.querySelector('.board');
    const existingForm = document.querySelector('.form-container');
    if (existingForm) existingForm.remove();

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
        formContainer.remove();
    });

    form.appendChild(button);
    formContainer.appendChild(form);
    board.appendChild(formContainer);
}