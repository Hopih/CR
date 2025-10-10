import { getBoards, createBoard, loadElements } from './api.js';
import { renderSticker } from './sticker.js';
import { clearBoardUI } from './board-utils.js';
import { initStickerSelection } from './selection.js';
import { updateBoardTitle } from './header.js';
import { showPrompt } from "./showPrompt.js";
import {showToast} from "./toast.js";

export async function openBoards() {
    const overlay = document.getElementById('boards-overlay');
    const grid = document.getElementById('boards-grid');
    const closeBtn = document.getElementById('close-boards');

    overlay.classList.add('show');
    grid.innerHTML = '';

    const boards = await getBoards();

    // 🔹 Плитка создания новой доски
    const newBoardBtn = document.createElement('div');
    newBoardBtn.className = 'board-thumb new-board';
    newBoardBtn.innerHTML = '<span class="plus-sign">+</span><div>New Board</div>';
    newBoardBtn.addEventListener('click', async () => {
        showPrompt("Enter new board name:", "Untitled Board", async (title) =>
        {
            const board = await createBoard(title);
            localStorage.setItem('boardId', board.id);
            localStorage.setItem('boardTitle', title);

            await switchBoard(board.id, title);
            overlay.classList.remove('show');
            showToast(`Создана доска: ${title}`, "success");
        });
    });
    grid.appendChild(newBoardBtn);

    // 🔹 Существующие доски
    boards.forEach(b => {
        const thumb = document.createElement('div');
        thumb.className = 'board-thumb';
        thumb.textContent = b.title;
        thumb.addEventListener('click', async () => {
            localStorage.setItem('boardId', b.id);
            localStorage.setItem('boardTitle', b.title);

            await switchBoard(b.id, b.title);
            overlay.classList.remove('show');
        });
        grid.appendChild(thumb);
    });

    closeBtn.addEventListener('click', () => overlay.classList.remove('show'));
}

async function switchBoard(boardId, boardTitle) {
    clearBoardUI(document.getElementById('board'));
    const elements = await loadElements(boardId);
    elements.forEach(await renderSticker);
    initStickerSelection();

    updateBoardTitle(boardTitle);
}