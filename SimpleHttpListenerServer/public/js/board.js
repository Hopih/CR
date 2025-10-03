import { createSticker } from './sticker.js';

export function initBoard() {
    const board = document.getElementById('board');
    const addStickerBtn = document.getElementById('addSticker');

    addStickerBtn.addEventListener('click', async () => {
        const sticker = await createSticker(board);
        board.appendChild(sticker);
    });

    window.onload = () => adjustHeadersFontSize();
    window.onresize = () => adjustHeadersFontSize();
}

function adjustHeadersFontSize() {
    const headers = document.querySelectorAll('header h2, footer h2');
    headers.forEach(header => adjustFontSizeHeaders(header, 10, 100));
}

function adjustFontSizeHeaders(element, min, max){
    let parent = element.parentElement;
    let fontSize = parseInt(window.getComputedStyle(element).fontSize);

    while (element.scrollWidth > parent.clientWidth || element.scrollHeight > parent.clientHeight) {
        fontSize -= 1;
        element.style.fontSize = fontSize + 'px';
        if (fontSize <= min) break;
    }

    while (element.scrollWidth <= parent.clientWidth && element.scrollHeight <= parent.clientHeight) {
        if (fontSize < max) {
            fontSize += 1;
            element.style.fontSize = fontSize + 'px';
        } else break;
    }
}