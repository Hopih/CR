import { LIMITS } from './model.js';

// 8. Создание локального стикера с необязательной цитатой
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

