import { LIMITS } from './model.js';
import { fitText } from './typography.js';
import { applyGeometry, addDragAndDrop,
    addResizeFeature } from './interactions.js';

export function renderSticker(board, model, onChange) {
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
    addDragAndDrop(board, sticker, model, onChange);
    addResizeFeature(board, sticker, handle, text, model, onChange);
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
