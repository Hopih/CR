import { saveElements } from "./api.js";

export async function createSticker(board) {
    const sticker = document.createElement('div');
    sticker.className = 'sticker';
    sticker.dataset.id = crypto.randomUUID(); // уникальный id

    try {
        const quoteResponse = await fetch("/api/quotes/random", {
            method: 'GET',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
        });

        if (quoteResponse.ok) {
            const quoteJson = await quoteResponse.json();
            sticker.innerHTML = quoteJson.quote;
        } else {
            sticker.innerHTML = "Новый стикер";
        }
    } catch {
        sticker.innerHTML = "Новый стикер";
    }

    const boardRect = board.getBoundingClientRect();
    const w = 200, h = 200;

    sticker.style.position = "absolute";
    sticker.style.width = w + "px";
    sticker.style.height = h + "px";
    sticker.style.top = Math.random() * (boardRect.height - h) + 'px';
    sticker.style.left = Math.random() * (boardRect.width - w) + 'px';

    makeDraggable(sticker);
    makeResizable(sticker);
    makeEditable(sticker);

    return sticker;
}

export function serializeStickers(boardId) {
    const stickers = [...document.querySelectorAll(".sticker")];
    return stickers.map(st => ({
        id: st.dataset.id,
        type: "sticker",
        x: parseInt(st.style.left),
        y: parseInt(st.style.top),
        width: parseInt(st.style.width),
        height: parseInt(st.style.height),
        zIndex: parseInt(st.style.zIndex || 0),
        rotation: 0,
        text: st.innerText,
        color: st.style.backgroundColor || "#ffe58a",
        fontSize: parseInt(st.style.fontSize || 16)
    }));
}

export async function saveBoard(boardId) {
    const elements = serializeStickers(boardId);
    await saveElements(boardId, elements);
}

function enableStickerTransition(sticker) {
    sticker.style.transition =
        "width 0.2s ease, height 0.2s ease, left 0.2s ease, top 0.2s ease, font-size 0.15s ease";
}

function disableStickerTransition(sticker) {
    sticker.style.transition = "none";
}

function makeDraggable(sticker) {
    let offsetX, offsetY;

    sticker.addEventListener('mousedown', function (e) {
        if (e.target.isContentEditable) return;

        const rect = sticker.getBoundingClientRect();
        offsetX = e.clientX - rect.left;
        offsetY = e.clientY - rect.top;

        disableStickerTransition(sticker);   // 🔥 отключаем плавность

        sticker.style.cursor = 'grabbing';
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });

    function onMouseMove(e) {
        const boardRect = sticker.parentElement.getBoundingClientRect();
        const left = e.clientX - boardRect.left - offsetX;
        const top  = e.clientY - boardRect.top  - offsetY;
        sticker.style.left = left + 'px';
        sticker.style.top  = top  + 'px';
    }

    function onMouseUp() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);

        enableStickerTransition(sticker);    // 🔥 возвращаем плавность
        sticker.style.cursor = 'grab';
    }
}

// --- ресайз ---
function makeResizable(sticker) {
    const resizer = document.createElement('div');
    resizer.className = 'resizer';
    sticker.appendChild(resizer);

    sticker.dataset.baseWidth = sticker.offsetWidth;
    sticker.dataset.baseHeight = sticker.offsetHeight;
    sticker.dataset.baseFontSize = parseInt(window.getComputedStyle(sticker).fontSize);

    resizer.addEventListener('mousedown', (e) => {
        e.preventDefault();
        window.addEventListener('mousemove', onMouseMove);
        window.addEventListener('mouseup', onMouseUp);
    });

    function onMouseMove(e) {
        const rect = sticker.getBoundingClientRect();
        let newWidth = e.clientX - rect.left;
        let newHeight = e.clientY - rect.top;

        newWidth = Math.max(newWidth, 50);
        newHeight = Math.max(newHeight, 50);

        sticker.style.width = newWidth + 'px';
        sticker.style.height = newHeight + 'px';

        const scale = newWidth / sticker.dataset.baseWidth;
        const newFontSize = sticker.dataset.baseFontSize * scale;
        sticker.style.fontSize = newFontSize + 'px';
    }

    function onMouseUp() {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
    }
}

// --- редактирование ---
function makeEditable(sticker) {
    let editor = null;
    let savedText = sticker.innerText.trim();
    const placeholderText = "Стикер пуст";

    function showDisplayMode() {
        sticker.innerHTML = savedText || placeholderText;
        sticker.style.textAlign = 'center';
        sticker.style.display = 'flex';
        sticker.style.alignItems = 'center';
        sticker.style.justifyContent = 'center';
        sticker.style.color = savedText ? "black" : "gray";
    }

    function showEditMode() {
        if (!editor) {
            editor = document.createElement('div');
            editor.innerText = savedText || "";
            editor.className = 'input';
            editor.contentEditable = "true";
            editor.style.display = 'flex';
            editor.style.alignItems = 'center';
            editor.style.justifyContent = 'center';
            editor.style.textAlign = 'center';

            editor.addEventListener('input', () => adjustFontSize(editor));
            editor.addEventListener('blur', () => {
                savedText = editor.innerText || "Стикер пуст";
                const appliedFontSize = window.getComputedStyle(editor).fontSize;
                sticker.innerHTML = savedText;
                sticker.style.fontSize = appliedFontSize;
                showDisplayMode();
            });
            editor.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    editor.blur();
                }
            });
        }
        sticker.innerHTML = '';
        sticker.appendChild(editor);
        editor.focus();
    }

    showDisplayMode();
    sticker.addEventListener('dblclick', () => showEditMode());
}

// --- автоадаптация текста ---
function adjustFontSize(el, minSize = 12, maxSize = 48) {
    const sticker = el.parentElement;

    // минимально допустимый размер контейнера (исходный)
    if (!sticker.dataset.initialWidth) {
        sticker.dataset.initialWidth  = sticker.offsetWidth;
        sticker.dataset.initialHeight = sticker.offsetHeight;
    }
    const initialW = parseFloat(sticker.dataset.initialWidth);
    const initialH = parseFloat(sticker.dataset.initialHeight);

    let fontSize = parseInt(getComputedStyle(el).fontSize);

    // шаговые коэффициенты
    const GROW_FACTOR   = 1.08; // +8% когда текста слишком много даже на min шрифте
    const SHRINK_FACTOR = 0.98; // -2% пошаговое сжатие контейнера
    const MAX_STEPS     = 50;

    // 1) уменьшать шрифт пока не поместится
    for (let i = 0; i < MAX_STEPS && isOverflow(el); i++) {
        if (fontSize > minSize) {
            fontSize--;
            el.style.fontSize = fontSize + 'px';
        } else break;
    }

    // 2) если даже на минимальном шрифте не влазит — увеличиваем контейнер (из центра)
    for (let i = 0; i < MAX_STEPS && isOverflow(el); i++) {
        const newW = sticker.offsetWidth  * GROW_FACTOR;
        const newH = sticker.offsetHeight * GROW_FACTOR;
        setSizeKeepCenter(sticker, newW, newH);
        void sticker.offsetWidth; // форсируем рефлоу
    }

    // 3) пошагово: +1px к шрифту → затем пытаемся ужать контейнер шагами, пока всё влазит
    for (let i = 0; i < MAX_STEPS; i++) {
        if (fontSize >= maxSize) break;

        // пробуем увеличить шрифт на 1
        const prevFont = fontSize;
        fontSize++;
        el.style.fontSize = fontSize + 'px';

        if (isOverflow(el)) {
            // не помещается — откат шрифта и стоп
            fontSize = prevFont;
            el.style.fontSize = fontSize + 'px';
            break;
        }

        // после успешного +1px шрифта — сжимаем контейнер маленькими шагами
        for (let s = 0; s < MAX_STEPS; s++) {
            const prevW = sticker.offsetWidth;
            const prevH = sticker.offsetHeight;

            // ниже исходного размера не уходим
            if (prevW <= initialW + 0.1 && prevH <= initialH + 0.1) break;

            const newW = Math.max(prevW * SHRINK_FACTOR, initialW);
            const newH = Math.max(prevH * SHRINK_FACTOR, initialH);

            setSizeKeepCenter(sticker, newW, newH);
            void sticker.offsetWidth;

            if (isOverflow(el)) {
                // перестало помещаться — откат последнего сжатия и заканчиваем «ужимание» на этом размере шрифта
                setSizeKeepCenter(sticker, prevW, prevH);
                break;
            }
            // иначе продолжаем ещё чуть сжимать на этом же размере шрифта
        }
        // и идём снова увеличивать шрифт на +1 (пока не упремся в max или переполнение)
    }
}

function setSizeKeepCenter(sticker, newWidth, newHeight) {
    const cw = sticker.offsetWidth;
    const ch = sticker.offsetHeight;

    // читаем реальные координаты через getBoundingClientRect и позицию родителя
    const parentRect = sticker.parentElement.getBoundingClientRect();
    const rect = sticker.getBoundingClientRect();

    const currentLeft = rect.left - parentRect.left;
    const currentTop  = rect.top  - parentRect.top;

    const cx = currentLeft + cw / 2;
    const cy = currentTop  + ch / 2;

    // аккуратно округляем до сотых, чтобы не накапливать субпиксели
    const round = (n) => Math.round(n * 100) / 100;

    sticker.style.width  = round(newWidth)  + 'px';
    sticker.style.height = round(newHeight) + 'px';
    sticker.style.left   = round(cx - newWidth  / 2) + 'px';
    sticker.style.top    = round(cy - newHeight / 2) + 'px';
}


function isOverflow(el, tol = 0) {
    return (el.scrollHeight - el.clientHeight > tol) ||
        (el.scrollWidth  - el.clientWidth  > tol);
}