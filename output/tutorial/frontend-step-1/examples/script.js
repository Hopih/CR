// 1. Модель и границы значений
const LIMITS = { minSize: 80, maxValue: 10000, maxText: 2000 };
const board = document.getElementById('board');
const boardModel = {
    id: null, title: 'Учебная доска', createdAt: null, stickers: []
};

function clampPixel(value, min, max = LIMITS.maxValue) {
    return Math.min(max, Math.max(min, Math.round(value)));
}

function toStickerPayload(model) {
    return {
        text: model.text, x: model.x, y: model.y,
        width: model.width, height: model.height
    };
}

function handleStickerChange(model) {
    // Позже здесь будет PUT. Сейчас проверяем контракт в консоли.
    console.log('Изменение завершено:', toStickerPayload(model));
}

// 2. Текст и представление
function fitText(element, min = 12, max = 36) {
    for (let size = max; size >= min; size--) {
        element.style.fontSize = `${size}px`;
        if (element.scrollHeight <= element.clientHeight &&
            element.scrollWidth <= element.clientWidth) return;
    }
}

function applyGeometry(sticker, model) {
    sticker.style.left = `${model.x}px`;
    sticker.style.top = `${model.y}px`;
    sticker.style.width = `${model.width}px`;
    sticker.style.height = `${model.height}px`;
}

function renderSticker(model, onChange) {
    const sticker = document.createElement('div');
    sticker.className = 'sticker';
    sticker.dataset.id = model.id;

    const text = document.createElement('div');
    text.className = 'sticker-text';
    text.textContent = model.text;

    const handle = document.createElement('div');
    handle.className = 'resize-handle';
    sticker.append(text, handle);
    applyGeometry(sticker, model);
    board.appendChild(sticker); // Нужен layout перед fitText.
    fitText(text);

    sticker.addEventListener('dblclick', event => {
        if (event.target.closest('.resize-handle')) return;
        editStickerText(sticker, text, model, onChange);
    });
    addDragAndDrop(sticker, model, onChange);
    addResizeFeature(sticker, handle, text, model, onChange);
    return sticker;
}

// 3. Редактор с завершением по blur
function editStickerText(sticker, text, model, onChange) {
    if (sticker.querySelector('textarea')) return;
    const originalText = model.text;
    const input = document.createElement('textarea');
    input.className = 'input';
    input.maxLength = LIMITS.maxText;
    input.value = originalText;
    input.setAttribute('aria-label', 'Текст стикера');
    text.replaceWith(input); // Ручка остается в DOM.
    input.focus();
    input.setSelectionRange(input.value.length, input.value.length);
    fitText(input);
    input.addEventListener('input', () => fitText(input));

    let cancelled = false;
    input.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            event.preventDefault();
            cancelled = true;
            input.blur();
        } else if (event.key === 'Enter' && event.ctrlKey) {
            event.preventDefault();
            input.blur();
        }
    });
    input.addEventListener('blur', () => {
        model.text = cancelled ? originalText : input.value;
        text.textContent = model.text;
        input.replaceWith(text);
        fitText(text);
        if (model.text !== originalText) onChange({ ...model });
    }, { once: true });
}

// 4. Перевод координат окна в координаты доски
function getBoardPoint(event) {
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
function addDragAndDrop(sticker, model, onChange) {
    sticker.addEventListener('mousedown', event => {
        if (event.button !== 0 ||
            event.target.closest('textarea, .resize-handle') ||
            sticker.querySelector('textarea')) return;
        event.preventDefault();
        const start = { x: model.x, y: model.y };
        const point = getBoardPoint(event);
        const offsetX = point.x - model.x;
        const offsetY = point.y - model.y;
        sticker.style.cursor = 'grabbing';

        trackMouse(moveEvent => {
            const next = getBoardPoint(moveEvent);
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
function addResizeFeature(sticker, handle, text, model, onChange) {
    handle.addEventListener('mousedown', event => {
        event.stopPropagation();
        if (event.button !== 0 || sticker.querySelector('textarea')) {
            return;
        }
        event.preventDefault();
        const point = getBoardPoint(event);
        const start = {
            width: model.width, height: model.height,
            x: point.x, y: point.y
        };

        trackMouse(moveEvent => {
            const next = getBoardPoint(moveEvent);
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

// 8. Создание локального стикера с необязательной цитатой
async function getInitialText() {
    try {
        const response = await fetch('/quote/random', {
            signal: AbortSignal.timeout(5000)
        });
        if (!response.ok) throw new Error('Цитата недоступна');
        const data = await response.json();
        if (typeof data.quote !== 'string' ||
            typeof data.author !== 'string') {
            throw new Error('Неверный формат цитаты');
        }
        return `${data.quote} (c) ${data.author}`
            .slice(0, LIMITS.maxText);
    } catch {
        return 'Новый стикер';
    }
}

async function createSticker() {
    const button = document.getElementById('addSticker');
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
        renderSticker(model, handleStickerChange);
    } finally {
        button.disabled = false;
    }
}

// 9. Запуск без перезаписи window.onload/window.onresize
document.getElementById('addSticker')
    .addEventListener('click', createSticker);
window.addEventListener('resize', () => {
    board.querySelectorAll('.sticker-text, .input').forEach(element => {
        fitText(element);
    });
});
