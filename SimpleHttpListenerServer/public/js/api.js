// public/js/api.js
const API_BASE = "/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    const headers = { "Content-Type": "application/json" };
    if (token) headers["Authorization"] = `Bearer ${token}`;
    return headers;
}

export async function signup(login, password) {
    const res = await fetch(`${API_BASE}/user/signup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ login, password })
    });
    return res.json();
}

export async function login(login, password) {
    const res = await fetch(`${API_BASE}/user/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ login, password })
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
    return await res.json();
}

export async function saveElements(boardId, elements) {
    const res = await fetch(`${API_BASE}/boards/${boardId}/elements/bulk`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ elements })
    });
    return await res.json();
}