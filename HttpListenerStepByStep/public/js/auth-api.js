import { request } from './http-client.js';
export const register = value => request('/api/users/register', 'POST', value);
export const login = value => request('/api/auth/login', 'POST', value);
export const me = () => request('/api/auth/me');
export const logout = () => request('/api/auth/logout', 'POST');