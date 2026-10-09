// 1. Модель и границы значений
export const LIMITS = { minSize: 80, maxValue: 10000, maxText: 2000 };

export function clampPixel(value, min, max = LIMITS.maxValue) {
    return Math.min(max, Math.max(min, Math.round(value)));
}

export function toStickerPayload(model) {
    return {
        text: model.text, x: model.x, y: model.y,
        width: model.width, height: model.height
    };
}

