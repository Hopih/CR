import { request } from './http-client.js';
export const register = value => request(
    '/api/users/register', 'POST', value);