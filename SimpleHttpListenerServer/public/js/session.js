export function isAuthenticated() {
    return !!localStorage.getItem('token');
}

export function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("boardId");
    localStorage.removeItem("guestKey");
    location.reload(); // обновляем страницу, чтобы создать гостевую
}