import {createSticker} from "./sticker.js";
import {saveElements, getCurrentElements} from "./api.js";


export function initToolBar(board) {
    const addStickerBtn = document.getElementById('addSticker');
    const saveButton = document.getElementById('saveButton');

    addStickerBtn.addEventListener('click', async () => {
        const sticker = await createSticker(board);
        board.appendChild(sticker);
    });

    saveButton.addEventListener('click', async () => {
        const boardId = localStorage.getItem("boardId");
        
        if (!boardId) {
            console.log("Нет активной доски для сохранения.");
        }

        const elements = getCurrentElements();
        if (!elements.length){
            console.log("Нет элементов для сохранения.");
        }

        await saveElements(boardId, elements);
        console.log("Доска сохранена.");
    });
}