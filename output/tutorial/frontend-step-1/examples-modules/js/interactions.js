import { LIMITS, clampPixel } from './model.js';
import { fitText } from './typography.js';

export function applyGeometry(sticker, model) {
    sticker.style.left = `${model.x}px`;
    sticker.style.top = `${model.y}px`;
    sticker.style.width = `${model.width}px`;
    sticker.style.height = `${model.height}px`;
}

// 4. Перевод координат окна в координаты доски
function getBoardPoint(board, event) {
    const rect = board.getBoundingClientRect();
    return {
        x: event.clientX - rect.left - board.clientLeft
            + board.scrollLeft,
        y: event.clientY - rect.top - board.clientTop
            + board.scrollTop
    };
}

// 5. Общая подписка на движение мыши до окончания действия
function trackMouse(onMove, onEnd) {
    function finish() {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', finish);
        window.removeEventListener('blur', finish);
        onEnd();
    }
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', finish);
    window.addEventListener('blur', finish);
}

// 6. Drag-and-drop
export function addDragAndDrop(board, sticker, model, onChange) {
    sticker.addEventListener('mousedown', event => {
        if (event.button !== 0 ||
            event.target.closest('textarea, .resize-handle') ||
            sticker.querySelector('textarea')) return;
        event.preventDefault();
        const start = { x: model.x, y: model.y };
        const point = getBoardPoint(board, event);
        const offsetX = point.x - model.x;
        const offsetY = point.y - model.y;
        sticker.style.cursor = 'grabbing';

        trackMouse(moveEvent => {
            const next = getBoardPoint(board, moveEvent);
            model.x = clampPixel(next.x - offsetX, 0);
            model.y = clampPixel(next.y - offsetY, 0);
            applyGeometry(sticker, model);
        }, () => {
            sticker.style.cursor = 'grab';
            if (model.x !== start.x || model.y !== start.y) {
                onChange({ ...model });
            }
        });
    });
}

// 7. Resize с одной ручкой и независимыми событиями
export function addResizeFeature(
    board, sticker, handle, text, model, onChange) {
    handle.addEventListener('mousedown', event => {
        event.stopPropagation();
        if (event.button !== 0 || sticker.querySelector('textarea')) {
            return;
        }
        event.preventDefault();
        const point = getBoardPoint(board, event);
        const start = {
            width: model.width, height: model.height,
            x: point.x, y: point.y
        };

        trackMouse(moveEvent => {
            const next = getBoardPoint(board, moveEvent);
            model.width = clampPixel(
                start.width + next.x - start.x, LIMITS.minSize);
            model.height = clampPixel(
                start.height + next.y - start.y, LIMITS.minSize);
            applyGeometry(sticker, model);
            fitText(text);
        }, () => {
            if (model.width !== start.width ||
                model.height !== start.height) onChange({ ...model });
        });
    });
}
