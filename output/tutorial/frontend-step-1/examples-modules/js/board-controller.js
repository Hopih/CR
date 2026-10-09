import { clampPixel, toStickerPayload } from './model.js';
import { getInitialText } from './api.js';
import { renderSticker } from './sticker-view.js';

export function createBoardController(board, button) {
    const boardModel = {
        id: null, title: 'Учебная доска',
        createdAt: null, stickers: []
    };

    function handleStickerChange(model) {
        // Здесь позднее появится PUT с полным телом запроса.
        console.log('Изменение завершено:', toStickerPayload(model));
    }

    async function createSticker() {
        button.disabled = true;
        try {
            const text = await getInitialText();
            const width = 200;
            const height = 200;
            const model = {
                // Это временный локальный id; позже id придет из POST.
                id: crypto.randomUUID(), boardId: boardModel.id,
                text,
                x: clampPixel(board.scrollLeft + Math.random() *
                    Math.max(0, board.clientWidth - width), 0),
                y: clampPixel(board.scrollTop + Math.random() *
                    Math.max(0, board.clientHeight - height), 0),
                width, height, createdAt: null
            };
            boardModel.stickers.push(model);
            renderSticker(board, model, handleStickerChange);
        } finally {
            button.disabled = false;
        }
    }

    return { createSticker };
}
