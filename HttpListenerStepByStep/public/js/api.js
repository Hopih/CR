import { request } from './http-client.js';
import { LIMITS } from './model.js';
export async function getInitialText() {
    try {
        const response = await fetch('/quote/random', {
            signal: AbortSignal.timeout(5000)
        });
        if (!response.ok) throw new Error('Цитата недоступна');
        const data = await response.json();
        if (typeof data.quote !== 'string' ||
            typeof data.author !== 'string') {
            throw new Error('Неверный формат цитаты');
        }
        return `${data.quote} (c) ${data.author}`
            .slice(0, LIMITS.maxText);
    } catch {
        return 'Новый стикер';
    }
}
export { ApiError } from './http-client.js';
const boardPath = id => `/api/boards/${encodeURIComponent(id)}`;
const stickerPath = (boardId, id) => `${boardPath(boardId)}/stickers/${encodeURIComponent(id)}`;
export const createBoard = title => request('/api/boards', 'POST', { title });
export const getBoard = id => request(boardPath(id));
export const deleteBoard = id => request(boardPath(id), 'DELETE');
export const createSticker = (id, value) => request(`${boardPath(id)}/stickers`, 'POST', value);
export const saveSticker = (boardId, id, value) => request(stickerPath(boardId, id), 'PUT', value);
export const deleteSticker = (boardId, id) => request(stickerPath(boardId, id), 'DELETE');
export const listMyBoards = () => request('/api/boards');

