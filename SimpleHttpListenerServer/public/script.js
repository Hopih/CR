document.getElementById('addSticker').addEventListener('click', createSticker);

window.onload = () => adjustHeadersFontSize();
window.onresize = () => adjustHeadersFontSize();

function adjustHeadersFontSize() {
    const headers = document.querySelectorAll('header h2, footer h2');
    headers.forEach(header => adjustFontSizeHeaders(header, 10, 100));
}

async function createSticker() {
    const sticker = document.createElement('div');
    sticker.className = 'sticker';
    
    let quoteResponse = await fetch("/quotes/random");
    if(quoteResponse.ok) {
        let quote = await quoteResponse.json();
        sticker.innerHTML = `${quote.content} (c) ${quote.originator.name}`;
    }
    
    const board = document.getElementById('board');
    const boardRect = board.getBoundingClientRect();

    const stickerWidth = 200;
    const stickerHeight = 200;

    sticker.style.top = Math.random() * (boardRect.height - stickerHeight) + 'px';
    sticker.style.left = Math.random() * (boardRect.width - stickerWidth) + 'px';

    board.appendChild(sticker);
    
    sticker.addEventListener('dblclick', () => addStickerText(sticker));
    addDragAndDrop(sticker);
    addResizeFeature(sticker);
}

function addDragAndDrop(sticker) {
    let offsetX, offsetY;

    sticker.addEventListener('mousedown', function(e) {
        if (e.target.tagName === 'TEXTAREA') return;

        offsetX = e.clientX - sticker.getBoundingClientRect().left;
        offsetY = e.clientY - sticker.getBoundingClientRect().top;

        sticker.style.cursor = 'grabbing';

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });

    function onMouseMove(e) {
        sticker.style.left = `${e.clientX - offsetX}px`;
        sticker.style.top = `${e.clientY - offsetY}px`;
    }

    function onMouseUp() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);

        sticker.style.cursor = 'grab';
    }
}

function addStickerText(sticker) {
    if (sticker.querySelector('textarea')) return;

    const input = document.createElement('textarea');
    input.className = 'input';
    input.rows = 4;
    input.value = sticker.innerText || 'Двойной клик для редактирования';
    
    sticker.innerHTML = '';
    sticker.appendChild(input);
    input.focus();

    adjustFontSize(input, 12, 36);

    input.addEventListener('input', () => adjustFontSize(input, 12, 36));

    document.addEventListener('click', function onClickOutside(event) {
        if (!sticker.contains(event.target)) {
            const currentFontSize = window.getComputedStyle(input).fontSize;

            // Применяем текст и размер шрифта, но не удаляем textarea
            input.value = input.value || 'Стикер пуст';
            sticker.style.fontSize = currentFontSize;

            document.removeEventListener('click', onClickOutside);
        }
    });
    addResizeFeature(sticker);
}

function adjustFontSize(element, minFontSize, maxFontSize) {
    let fontSize = parseInt(window.getComputedStyle(element).fontSize);

    while (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth) {
        if (fontSize <= minFontSize) break;
        fontSize -= 1;
        element.style.fontSize = fontSize + 'px';
    }

    while (element.scrollHeight <= element.clientHeight && element.scrollWidth <= element.clientWidth) {
        if (fontSize >= maxFontSize) break;
        fontSize += 1;
        element.style.fontSize = fontSize + 'px';
    }
}


function adjustFontSizeHeaders(element, min, max){
    
    let parent = element.parentElement;

    // Начальный размер шрифта
    let fontSize = parseInt(window.getComputedStyle(element).fontSize);
    const minFontSize = min; // Минимальный размер шрифта
    const maxFontSize = max; // Максимальный размер шрифта (можно настроить по желанию)

    // Уменьшаем размер шрифта до тех пор, пока текст не поместится
    while (element.scrollWidth > parent.clientWidth || element.scrollHeight > parent.clientHeight) {
        fontSize -= 1; // Уменьшаем размер шрифта на один пиксель
        element.style.fontSize = fontSize + 'px';

        if (fontSize <= minFontSize) { // Минимальный размер шрифта
            break;
        }
    }

    // Увеличиваем размер шрифта до тех пор, пока текст помещается в родительский элемент
    while (element.scrollWidth <= parent.clientWidth && element.scrollHeight <= parent.clientHeight) {
        if (fontSize < maxFontSize) { // Проверяем максимальный размер шрифта
            fontSize += 1; // Увеличиваем размер шрифта на один пиксель
            element.style.fontSize = fontSize + 'px';
        } else {
            break; // Если достигнут максимальный размер, выходим из цикла
        }
    }
}

function addResizeFeature(sticker) {
    // Создаем элемент для изменения размера
    const resizeHandle = document.createElement('div');
    resizeHandle.className = 'resize-handle';
    sticker.appendChild(resizeHandle);

    let startWidth, startHeight, startX, startY;

    resizeHandle.addEventListener('mousedown', function(e) {
        e.preventDefault();
        startWidth = sticker.offsetWidth;
        startHeight = sticker.offsetHeight;
        startX = e.clientX;
        startY = e.clientY;

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });

    function onMouseMove(e) {
        // Новые размеры стикера
        const newWidth = startWidth + (e.clientX - startX);
        const newHeight = startHeight + (e.clientY - startY);

        // Масштабный коэффициент
        const scale = newWidth / startWidth;

        // Применяем новые размеры и пропорционально изменяем размер шрифта
        sticker.style.width = `${newWidth}px`;
        sticker.style.height = `${newHeight}px`;
        sticker.style.fontSize = `${parseInt(window.getComputedStyle(sticker).fontSize) * scale}px`;

        // Если есть textarea, масштабируем и его
        const input = sticker.querySelector('textarea');
        if (input) {
            input.style.width = '100%';
            input.style.height = '100%';
            adjustFontSize(input, 12, 36);
        }
    }

    function onMouseUp() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }
}