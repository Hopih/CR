// 2. Текст и представление
export function fitText(element, min = 12, max = 36) {
    for (let size = max; size >= min; size--) {
        element.style.fontSize = `${size}px`;
        if (element.scrollHeight <= element.clientHeight &&
            element.scrollWidth <= element.clientWidth) return;
    }
}

