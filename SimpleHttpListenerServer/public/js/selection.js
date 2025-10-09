export function initStickerSelection() {
    const board = document.querySelector('.board');
    if (!board) return;

    board.addEventListener('click', (e) => {
        const target = e.target.closest('.sticker');
        const selected = document.querySelector('.sticker.selected');

        // если клик не по стикеру — снимаем выделение
        if (!target) {
            if (selected) selected.classList.remove('selected');
            return;
        }

        // если клик по уже выделенному — снять выделение
        if (selected === target) {
            target.classList.remove('selected');
            return;
        }

        // снять выделение со старого и выделить новый
        if (selected) selected.classList.remove('selected');
        target.classList.add('selected');
    });

    // --- Удаление выделенного по клавише Delete ---
    document.addEventListener('keydown', (e) => {
        const selected = document.querySelector('.sticker.selected');
        if (!selected) return;

        // 1️⃣ Если пользователь редактирует текст — не удаляем
        const active = document.activeElement;
        if (active && active.isContentEditable) return;

        // 2️⃣ Только Delete или Backspace
        if (e.key === 'Delete') {
            e.preventDefault(); // не прокручиваем страницу
            selected.classList.add('removed');
            setTimeout(() => selected.remove(), 150); // плавное удаление
            console.log('🗑️ Стикер удалён локально. Сохраните доску, чтобы применить изменения.');
        }
    });
}