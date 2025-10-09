// Очистка текущей доски (все элементы)
export function clearBoardUI(boardElement) {
    const stickers = boardElement.querySelectorAll('.board-item, .sticker');
    stickers.forEach(s => s.remove());
}