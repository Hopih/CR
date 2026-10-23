import { initializeUserHeader } from './user-header.js';
import { createBoardController } from './board-controller.js';
import { fitText } from './typography.js';
import { createPopup } from './popup.js';
const board = document.getElementById('board');
const ui = {
    add: document.getElementById('addSticker'),
    select: document.getElementById('boardSelect'),
    newBoard: document.getElementById('newBoard'),
    openBoard: document.getElementById('openBoard'),
    removeBoard: document.getElementById('deleteBoard'),
    reload: document.getElementById('reloadBoard'),
    title: document.getElementById('boardTitle'),
    status: document.getElementById('boardStatus')
};

const controller = createBoardController(board, ui);
const openPopup = createPopup(document.getElementById('boardPopup'));

ui.add.addEventListener('click', controller.createSticker);

ui.newBoard.addEventListener('click', async () => {
    const title = await openPopup({
        title: 'Новая доска', label: 'Название доски',
        value: 'Новая доска', maxLength: 120, accept: 'Создать',
        validate: value => !value || value.length > 120
            ? 'Введите название от 1 до 120 символов.' : ''
    });
    if (title !== null) await controller.newBoard(title);
});

ui.reload.addEventListener('click', controller.reload);

ui.removeBoard.addEventListener('click', async () => {
    const accepted = await openPopup({
        title: 'Удалить доску?',
        description: 'Доска и все её стикеры будут удалены. Это действие нельзя отменить.',
        confirm: true, danger: true, accept: 'Удалить'
    });
    if (accepted) await controller.removeBoard();
});

ui.select.addEventListener('change', () => controller.openBoard(ui.select.value));

ui.openBoard.addEventListener('click', async () => {
    const id = await openPopup({
        title: 'Открыть доску', label: 'UUID гостевой доски',
        maxLength: 36, accept: 'Открыть',
        validate: value =>
            /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
            && value !== '00000000-0000-0000-0000-000000000000'
                ? '' : 'Введите корректный UUID доски.'
    });
    if (id !== null) await controller.openBoard(id);
});

window.addEventListener('resize', () => {
    board.querySelectorAll('.sticker-text, .input').forEach(element => fitText(element));
});
let starting = true;

window.addEventListener('session-changed', () => { if (!starting) location.reload(); });
try {
    const session = await initializeUserHeader();
    if (session) await controller.initialize(session.user.id);
    else ui.status.textContent = 'Войдите, чтобы открыть свои доски';
} 
catch (ex) {
    ui.status.textContent = ex.message; 
}
finally {
    starting = false;
}