import { createGuestBoard, loadElements, validateGuestBoard } from "./api.js";
import { renderSticker } from "./sticker.js";
import {renderAuthButtons } from "./auth.js";
import {initToolBar } from "./toolbar.js";

const board = document.getElementById("board");

async function init() {

    let boardId = localStorage.getItem("boardId");

    // Если ничего нет — создаём новую
    if (!boardId) {
        console.log("🔹 Нет доски, создаём новую...");
        const { boardId: newId, tempKey } = await createGuestBoard();
        localStorage.setItem("boardId", newId);
        localStorage.setItem("guestKey", tempKey);
        boardId = newId;
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

    // Подгружаем элементы доски
    const elements = await loadElements(boardId);
    elements.forEach(renderSticker);

    initToolBar(board);

    renderAuthButtons();
}

window.addEventListener("DOMContentLoaded", init);
