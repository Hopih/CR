import { login, signup, getBoards, createBoard, createGuestBoard, loadElements } from "./api.js";
import { createSticker, saveBoard } from "./sticker.js";
import {renderAuthButtons } from "./auth.js";

const board = document.getElementById("board");

async function init() {
    let boardId = localStorage.getItem("boardId");

    // если не авторизован и нет доски → создаём гостевую
    if (!localStorage.getItem("token") && !boardId) {
        const guest = await createGuestBoard();
        boardId = guest.boardId;
    }

    // загрузка элементов
    const elements = await loadElements(boardId);
    for (const e of elements) {
        if (e.type === "sticker") {
            let sticker = await createSticker(board);
            board.appendChild(sticker);
        }
    }

    const saveBtn = document.getElementById("saveBoard");
    if (saveBtn) {
        saveBtn.addEventListener("click", async () => {
            await saveBoard(boardId);
            alert("Сохранено!");
        });
    }

    // кнопка "добавить стикер"
    document.getElementById("addSticker").addEventListener("click", async () => {
        let sticker = await createSticker(board);
        board.appendChild(sticker);
    });

    renderAuthButtons();
}

window.addEventListener("DOMContentLoaded", init);
