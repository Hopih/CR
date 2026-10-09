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

export class ApiError extends Error {
    constructor(status, message) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}

async function request(path, method = 'GET', body) {
    const response = await fetch(path, {
        method,
        headers: body === undefined ? {} : {
            'Content-Type': 'application/json'
        },
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: AbortSignal.timeout(10000)
    });
    
    if (!response.ok) {
        let message = `Ошибка HTTP ${response.status}`;
        try {
            const data = await response.json();
            message = data.error?.message || message;
        } 
        catch {
            /* Ответ мог прийти не в JSON. */ 
        }
        throw new ApiError(response.status, message);
    }
    
    if (response.status === 204) return;
    
    return response.json();
}

const boardPath = id => `/api/boards/${encodeURIComponent(id)}`;
const stickerPath = (boardId, id) =>
    `${boardPath(boardId)}/stickers/${encodeURIComponent(id)}`;

export const createBoard = title => request('/api/boards', 'POST', { title });
export const getBoard = id => request(boardPath(id));
export const deleteBoard = id => request(boardPath(id), 'DELETE');
export const createSticker = (id, value) =>
    request(`${boardPath(id)}/stickers`, 'POST', value);
export const saveSticker = (boardId, id, value) =>
    request(stickerPath(boardId, id), 'PUT', value);
export const deleteSticker = (boardId, id) =>
    request(stickerPath(boardId, id), 'DELETE');

