// public/js/api.js
const API_BASE = "/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    const headers = { "Content-Type": "application/json" };
    if (token) headers["Authorization"] = `Bearer ${token}`;
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
    const res = await fetch(`${API_BASE}/boards`, {
        method: "GET",
        headers: authHeaders()
    });
    return res.json();
}

export async function createBoard(title) {
    const res = await fetch(`${API_BASE}/boards`, {
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
    return data;
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
    const res = await fetch(`${API_BASE}/boards/${boardId}/elements/bulk`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ elements })
    });
    return await res.json();
}


export function getCurrentElements() {
    const items = document.querySelectorAll('.board-item');
    return Array.from(items).map(item => {
        const base = {
            id: item.dataset.id,
            x: parseInt(item.style.left) || 0,
            y: parseInt(item.style.top) || 0,
            width: parseInt(item.style.width) || 0,
            height: parseInt(item.style.height) || 0,
            type: item.classList.contains('sticker') ? 'sticker'
                : item.classList.contains('image') ? 'image'
                    : 'text'
        };

        if (base.type === 'sticker') {
            base.text = item.textContent.trim();
            base.color = item.dataset.color || null;
        } else if (base.type === 'image') {
            base.src = item.dataset.src || null;
        }

        return base;
    });
}