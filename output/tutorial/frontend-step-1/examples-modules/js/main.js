import { createBoardController } from './board-controller.js';
import { fitText } from './typography.js';

const board = document.getElementById('board');
const button = document.getElementById('addSticker');
const controller = createBoardController(board, button);

button.addEventListener('click', controller.createSticker);
window.addEventListener('resize', () => {
    board.querySelectorAll('.sticker-text, .input').forEach(element => {
        fitText(element);
    });
});
