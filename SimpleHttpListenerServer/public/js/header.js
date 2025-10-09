export function updateBoardTitle(title) {
    const h2 = document.querySelector('header h2');
    if (h2) {
        h2.textContent = title || "My Board";
    }
}