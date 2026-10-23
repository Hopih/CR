import { clampPixel, toStickerPayload } from './model.js';
import * as api from './api.js';
import * as storage from './board-storage.js';
import { renderSticker } from './sticker-view.js';
import { applyGeometry } from './interactions.js';
import { fitText } from './typography.js';
export function createBoardController(board, ui) {
    let boardModel = null;
    let accountId = null;
    let busy = false;
    let operations = Promise.resolve();
    let pending = 0;
    let batchError = null;
    const confirmed = new Map();
    const revisions = new Map();
    const views = new Map();

    function status(message) {
        ui.status.textContent = message;
    }

    function refreshList() {
        const local = storage.readBoards();
        ui.select.replaceChildren();
        for (const id of local.boardIds) {
            const option = document.createElement('option');
            option.value = id;
            option.textContent = id === boardModel?.id
                ? `${boardModel.title} (${id.slice(0, 8)})` : id.slice(0, 8);
            ui.select.appendChild(option);
        }
        ui.select.value = boardModel?.id ?? '';
        ui.title.textContent = boardModel?.title ?? 'Учебная доска';
    }

    function lock(value) {
        busy = value;
        ui.add.disabled = value || !boardModel;
        ui.select.disabled = value || !boardModel;
        ui.removeBoard.disabled = value || !boardModel;
        ui.newBoard.disabled = value || !accountId;
        ui.openBoard.disabled = value || !accountId;
        ui.reload.disabled = value || !accountId;
        for (const element of views.values()) {
            element.dataset.busy = String(value);
            element.querySelectorAll('button').forEach(b => b.disabled = value);
        }
    }

    function run(action) {
        // blur сохраняет текст до переключения/удаления доски.
        // Обработчик редактора удаляет textarea до вызова saveChange.
        board.querySelectorAll('textarea').forEach(input => input.blur());
        if (pending === 0) batchError = null;
        pending++;
        lock(true);
        status('Сохранение…');
        const task = operations.then(async () => {
            try {
                await action();
            } catch (error) {
                batchError = error;
            } finally {
                pending--;
                if (pending === 0) {
                    lock(false);
                    refreshList();
                    status(batchError
                        ? `${batchError.message}. Используйте «Перечитать» перед повтором.`
                        : storage.canPersistBoards() ? 'Сохранено'
                            : 'Список досок доступен только до закрытия страницы');
                }
            }
        });
        operations = task;
        return task;
    }

    function showModel(value) {
        board.querySelectorAll('.sticker').forEach(element => element.remove());
        boardModel = value;
        confirmed.clear();
        revisions.clear();
        views.clear();
        for (const model of boardModel.stickers) attach(model);
        storage.rememberBoard(boardModel.id);
        refreshList();
    }

    function attach(model) {
        confirmed.set(model.id, {...model});
        const element = renderSticker(board, model, saveChange, removeSticker);
        views.set(model.id, element);
        element.dataset.busy = String(busy);
        element.querySelectorAll('button').forEach(b => b.disabled = busy);
    }

    async function loadKnownBoard() {
        const items = await api.listMyBoards();
        // Серверный список, а не UUID прежнего пользователя из localStorage.
        for (const id of storage.readBoards().boardIds) storage.forgetBoard(id);
        for (const item of items) storage.rememberBoard(item.id);
        if (items.length) showModel(await api.getBoard(items[0].id));
        else {
            boardModel = null;
            board.querySelectorAll('.sticker').forEach(e => e.remove());
            views.clear();
            confirmed.clear();
            revisions.clear();
        }
    }

    async function saveChange(snapshot) {
        const targetBoard = boardModel;
        const model = targetBoard?.stickers.find(x => x.id === snapshot.id);
        if (!model) return;
        const revision = (revisions.get(model.id) ?? 0) + 1;
        revisions.set(model.id, revision);
        await run(async () => {
            const previous = confirmed.get(snapshot.id);
            try {
                await api.saveSticker(targetBoard.id, model.id,
                    toStickerPayload(snapshot));
                confirmed.set(model.id, {...snapshot});
                if (revisions.get(model.id) === revision) {
                    Object.assign(model, snapshot);
                    updateView(model);
                }
            } catch (error) {
                // Ошибка старого запроса не должна затирать более новое изменение.
                if (revisions.get(model.id) === revision) {
                    Object.assign(model, previous);
                    updateView(model);
                }
                throw error;
            }
        });
    }

    function updateView(model) {
        const element = views.get(model.id);
        if (!element) return;
        applyGeometry(element, model);
        const text = element.querySelector('.sticker-text');
        text.textContent = model.text;
        fitText(text);
    }

    async function removeSticker(id) {
        await run(async () => {
            await api.deleteSticker(boardModel.id, id);
            boardModel.stickers = boardModel.stickers.filter(x => x.id !== id);
            views.get(id)?.remove();
            views.delete(id);
            confirmed.delete(id);
            revisions.delete(id);
        });
    }

    async function createSticker() {
        if (!boardModel) return;
        await run(async () => {
            const width = 200;
            const height = 200;
            const payload = {
                text: await api.getInitialText(),
                x: clampPixel(board.scrollLeft + Math.random() *
                    Math.max(0, board.clientWidth - width), 0),
                y: clampPixel(board.scrollTop + Math.random() *
                    Math.max(0, board.clientHeight - height), 0),
                width, height
            };
            const model = await api.createSticker(boardModel.id, payload);
            boardModel.stickers.push(model);
            attach(model);
        });
    }

    async function newBoard(title) {
        if (typeof title !== 'string') return;
        const name = title.trim();
        if (!name || name.length > 120) {
            status('Название должно содержать от 1 до 120 символов');
            return;
        }
        await run(async () => {
            showModel(await api.createBoard(name));
        });
    }

    async function openBoard(id) {
        if (!id) return;
        await run(async () => showModel(await api.getBoard(id.trim())));
    }

    async function removeBoard() {
        if (!boardModel) return;
        await run(async () => {
            const id = boardModel.id;
            try {
                await api.deleteBoard(id);
            } catch (error) {
                if (!(error instanceof api.ApiError && error.status === 404)) {
                    throw error;
                }
            }
            storage.forgetBoard(id);
            boardModel = null;
            board.querySelectorAll('.sticker').forEach(element => element.remove());
            views.clear();
            confirmed.clear();
            revisions.clear();
            await loadKnownBoard();
        });
    }

    function reload() {
        return run(loadKnownBoard);
    }

    lock(true);
    busy = false;
    return {
        initialize: userId => {
            accountId = userId;
            return reload();
        },
        reload, createSticker,
        newBoard, openBoard, removeBoard
    };
}
