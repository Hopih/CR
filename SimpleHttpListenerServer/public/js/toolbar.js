import {createSticker} from "./sticker";
import {saveElements} from "./api.js";


export function initToolBar(board) {
    const addStickerBtn = document.getElementById('addSticker');
    const saveButton = document.getElementById('saveButton');

    addStickerBtn.addEventListener('click', async () => {
        const sticker = await createSticker(board);
        board.appendChild(sticker);
    });

    saveButton.addEventListener('click', async () => {
        const result = await saveElements(board.id, );
        board.appendChild(sticker);
    });
}