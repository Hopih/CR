export function showToast(message, type = "info", duration = 3000) {
    // контейнер для всех уведомлений
    let container = document.querySelector(".toast-container");
    if (!container) {
        container = document.createElement("div");
        container.className = "toast-container";
        document.body.appendChild(container);
    }

    // само уведомление
    const toast = document.createElement("div");
    toast.className = `toast toast-${type}`;
    toast.textContent = message;

    container.appendChild(toast);

    // плавное появление
    requestAnimationFrame(() => toast.classList.add("show"));

    // удаление через заданное время
    setTimeout(() => {
        toast.classList.remove("show");
        toast.classList.add("hide");
        toast.addEventListener("transitionend", () => toast.remove());
    }, duration);
}