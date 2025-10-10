let selectionInitialized = false;

export function initStickerSelection() {
    const board = document.querySelector('.board');
    if (!board || selectionInitialized) return;
    selectionInitialized = true; // чтобы не навешивать повторно

    let dragStarted = false;
    let dragStartPos = { x: 0, y: 0 };

    // обработка выделения
    board.addEventListener('mousedown', (e) => {
        if (e.target.closest('.sticker')) return; // позволяем drag
        dragStarted = false;
        dragStartPos = { x: e.clientX, y: e.clientY };
    });

    board.addEventListener('mousemove', (e) => {
        if (e.target.closest('.sticker')) return;
        if (Math.abs(e.clientX - dragStartPos.x) > 3 || Math.abs(e.clientY - dragStartPos.y) > 3)
            dragStarted = true;
    });

    board.addEventListener('mouseup', (e) => {
        if (dragStarted) {
            dragStarted = false;
            return;
        }

        const target = e.target.closest('.sticker');
        const selected = document.querySelector('.sticker.selected');

        if (!target) {
            selected?.classList.remove('selected');
            return;
        }

        if (selected === target) {
            target.classList.remove('selected');
            return;
        }

        selected?.classList.remove('selected');
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