export function showPrompt(title, placeholder = "", onConfirm = null, onCancel = null) {
    // Создаём overlay
    const overlay = document.createElement("div");
    overlay.className = "form-overlay show";

    // Создаём контейнер
    const modal = document.createElement("div");
    modal.className = "form-container";

    const heading = document.createElement("h2");
    heading.textContent = title;
    modal.appendChild(heading);

    // Поле ввода
    const input = document.createElement("input");
    input.type = "text";
    input.placeholder = placeholder;
    input.autofocus = true;
    modal.appendChild(input);

    // Кнопки
    const buttons = document.createElement("div");
    buttons.className = "form-buttons";

    const okBtn = document.createElement("button");
    okBtn.textContent = "Создать";
    okBtn.className = "confirm";

    const cancelBtn = document.createElement("button");
    cancelBtn.textContent = "Отмена";
    cancelBtn.className = "cancel";

    buttons.append(okBtn, cancelBtn);
    modal.appendChild(buttons);

    overlay.appendChild(modal);
    document.body.appendChild(overlay);

    // События
    okBtn.addEventListener("click", () => {
        const value = input.value.trim();
        if (value && onConfirm) onConfirm(value);
        overlay.remove();
    });

    cancelBtn.addEventListener("click", () => {
        if (onCancel) onCancel();
        overlay.remove();
    });

    overlay.addEventListener("click", (e) => {
        if (e.target === overlay) {
            overlay.remove();
            if (onCancel) onCancel();
        }
    });

    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter") okBtn.click();
        if (e.key === "Escape") cancelBtn.click();
    });
}