const KEY = 'httplistener.boards.v1';
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
let memory = { boardIds: [], activeBoardId: null };
let storageAvailable = true;

function normalize(value) {
    const ids = Array.isArray(value?.boardIds)
        ? value.boardIds.filter(id => typeof id === 'string' && uuid.test(id))
        : [];
    const boardIds = [...new Set(ids.map(id => id.toLowerCase()))];
    const active = typeof value?.activeBoardId === 'string'
        ? value.activeBoardId.toLowerCase() : null;
    return {
        boardIds,
        activeBoardId: boardIds.includes(active) ? active : null
    };
}

export function readBoards() {
    try {
        const raw = localStorage.getItem(KEY);
        memory = normalize(raw ? JSON.parse(raw) : memory);
    } 
    catch {
        storageAvailable = false;
    }
    return {
        ...memory, boardIds: [...memory.boardIds] 
    };
}

function write(value) {
    memory = normalize(value);
    try { 
        localStorage.setItem(KEY, JSON.stringify(memory)); 
    }
    catch {
        storageAvailable = false; 
    }
}

export function rememberBoard(id) {
    const state = readBoards();
    const normalizedId = id.toLowerCase();
    write({
        boardIds: [...state.boardIds, normalizedId],
        activeBoardId: normalizedId
    });
}

export function forgetBoard(id) {
    const state = readBoards();
    write({
        boardIds: state.boardIds.filter(value => value !== id),
        activeBoardId: state.activeBoardId === id ? null : state.activeBoardId
    });
}

export const canPersistBoards = () => storageAvailable;