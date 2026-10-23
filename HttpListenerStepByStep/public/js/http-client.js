export class ApiError extends Error {
    constructor(status, message) { super(message); this.status = status; }
}
export async function request(path, method = 'GET', body) {
    const response = await fetch(path, {
        method, credentials: 'same-origin',
        headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: AbortSignal.timeout(10000)
    });
    if (!response.ok) {
        let message = `Ошибка HTTP ${response.status}`;
        try { message = (await response.json()).error?.message || message; } catch {}
        throw new ApiError(response.status, message);
    }
    return response.status === 204 ? undefined : response.json();
}