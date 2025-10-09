import { safeFetch } from './safeFetch.js';

const API_BASE = "/api";

function authHeaders() {
    const headers = { "Content-Type": "application/json" };
    const token = localStorage.getItem("token");
    
    if (token){
        headers["Authorization"] = `Bearer ${token}`;  
    } 
    
    const guestKey = localStorage.getItem("guestKey");
    
    if (guestKey){
        headers["X-Guest-Key"] = guestKey;
    } 
    
    return headers;
}

export async function signup(user) {
    const res = await fetch(`${API_BASE}/user/signup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(user)
    });
    return res.json();
}

export async function login(user) {
    const res = await fetch(`${API_BASE}/user/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(user)
    });
    const data = await res.json();
    if (res.ok) {
        localStorage.setItem("token", data.token);
    }
    return data;
}

export async function getBoards() {
    const res = await safeFetch(`${API_BASE}/boards`, {
        method: "GET",
        headers: authHeaders()
    });
    return res.json();
}

export async function createBoard(title) {
    const res = await safeFetch(`${API_BASE}/boards`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ title })
    });
    return res.json();
}

export async function createGuestBoard(title = "Guest Board") {
    const res = await fetch(`${API_BASE}/guest/board`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title })
    });
    const data = await res.json();
    localStorage.setItem("boardId", data.boardId);
    localStorage.setItem("guestKey", data.tempKey);
    return data;
}

export async function validateGuestBoard() {
    const boardId = localStorage.getItem("boardId");
    if (!boardId) return false;

    const res = await fetch(`/api/boards/${boardId}/elements`, {
        headers: authHeaders(),
    });

    // доступен — возвращаем true
    if (res.ok) return true;

    // 403 → ключ просрочен
    if (res.status === 403) {
        console.warn("Guest board expired or invalid key. Clearing localStorage...");
        localStorage.removeItem("boardId");
        localStorage.removeItem("guestKey");
        return false;
    }

    return false;
}

export async function loadElements(boardId) {
    const res = await fetch(`${API_BASE}/boards/${boardId}/elements`, {
        headers: authHeaders()
    });
    if(res.ok) {
        return await res.json();
    }
    return [];
}

export async function saveElements(boardId, elements) {
    const res = await safeFetch(`${API_BASE}/boards/${boardId}/elements/bulk`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ elements })
    });
    return await res.json();
}


export function getCurrentElements() {
    const items = document.querySelectorAll('.board-item');
    return Array.from(items).map(item => ({
        id: item.dataset.id || crypto.randomUUID(),
        type: item.classList.contains('sticker') ? 'sticker' : 'unknown',
        x: parseInt(item.style.left) || 0,
        y: parseInt(item.style.top) || 0,
        width: parseInt(item.style.width) || 0,
        height: parseInt(item.style.height) || 0,
        text: item.textContent.trim(),
        color: item.dataset.color
            || item.style.backgroundColor
            || getComputedStyle(item).backgroundColor
            || null,
        fontSize: parseInt(item.style.fontSize) || 14,
        rotation: parseFloat(item.style.rotate) || 0,
        zIndex: parseInt(item.style.zIndex) || 0
    }));
}