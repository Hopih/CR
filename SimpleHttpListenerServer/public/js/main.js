import { createGuestBoard, loadElements, validateGuestBoard } from "./api.js";
import { renderSticker } from "./sticker.js";
import {renderAuthButtons } from "./auth.js";
import {initToolBar } from "./toolbar.js";
import { initStickerSelection } from './selection.js';
import { updateBoardTitle } from './header.js';

const board = document.getElementById("board");

async function init() {

    let boardId = localStorage.getItem("boardId");
    let boardTitle = localStorage.getItem("boardTitle");

    // Если ничего нет — создаём новую
    if (!boardId) {
        console.log("🔹 Нет доски, создаём новую...");
        const { boardId: newId, tempKey } = await createGuestBoard();
        localStorage.setItem("boardId", newId);
        localStorage.setItem("guestKey", tempKey);
        boardId = newId;
        boardTitle = "Guest Board";
        localStorage.setItem("boardTitle", boardTitle);
    }
    else {
        // Проверяем, не просрочен ли guestKey
        const isValid = await validateGuestBoard();
        if (!isValid) {
            console.log("🔄 Старый ключ недействителен, создаём новую доску...");
            const { boardId: newId, tempKey } = await createGuestBoard();
            localStorage.setItem("boardId", newId);
            localStorage.setItem("guestKey", tempKey);
            boardId = newId;
        }
    }

    updateBoardTitle(boardTitle);

    // Подгружаем элементы доски
    const elements = await loadElements(boardId);
    elements.forEach(renderSticker);


    initStickerSelection();
    initToolBar(board);
    renderAuthButtons();
}

window.addEventListener("DOMContentLoaded", init);
