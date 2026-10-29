const refreshButton = document.querySelector('#refresh');
const statusElement = document.querySelector('#status');
const ratesBody = document.querySelector('#rates');
const dateElement = document.querySelector('#rates-date');
const numberFormat = new Intl.NumberFormat('ru-RU', { minimumFractionDigits: 4, maximumFractionDigits: 4 });
const convertForm = document.querySelector('#convert-form');
const fromSelect = document.querySelector('#from');
const toSelect = document.querySelector('#to');
const convertResult = document.querySelector('#convert-result');
const sortButtons = document.querySelectorAll('.sort');

// Текущая сортировка. Клик по заголовку меняет эти две переменные и перезагружает таблицу.
let sort = 'code';
let order = 'asc';

async function loadRates() {
  refreshButton.disabled = true;
  statusElement.textContent = 'Загрузка…';
  try {
    // URLSearchParams соберёт ?sort=rate&order=desc
    const response = await fetch(`/api/rates?${new URLSearchParams({ sort, order })}`);
    const data = await response.json();
    if (!response.ok) throw new Error(data.error || 'Не удалось загрузить курсы.');

    const rows = data.currencies.map(currency => {
      const row = document.createElement('tr');
      for (const value of [currency.charCode, currency.name, currency.nominal, numberFormat.format(currency.value)]) {
        const cell = document.createElement('td');
        cell.textContent = value;
        row.append(cell);
      }
      return row;
    });
    ratesBody.replaceChildren(...rows);
    showSortArrows();
    fillCurrencySelects(data.currencies);   // списки валют — из тех же данных, что и таблица
    const date = new Date(data.date).toLocaleDateString('ru-RU', { timeZone: 'Europe/Moscow' });
    dateElement.textContent = `Курсы на ${date}`;
    statusElement.textContent = `Загружено валют: ${rows.length}`;
  } catch (error) {
    statusElement.textContent = `${error.message} Если таблица уже была загружена, в ней остались прежние данные.`;
  } finally {
    refreshButton.disabled = false;
  }
}

// Заполняет оба списка: RUB + все коды из таблицы. Вызывается из loadRates().
function fillCurrencySelects(currencies) {
  const codes = ['RUB', ...currencies.map(c => c.charCode)];  // RUB + все коды из таблицы
  const fromValue = fromSelect.value || 'USD';   // что было выбрано; при первой загрузке — USD
  const toValue = toSelect.value || 'RUB';       // ...и RUB

  fromSelect.innerHTML = '';   // очистить список "из"
  toSelect.innerHTML = '';     // очистить список "в"

  for (const code of codes) {                     // для каждого кода валюты...
    fromSelect.append(new Option(code, code));    // ...добавить вариант в список "из"
    toSelect.append(new Option(code, code));      // ...и в список "в"
  }

  // вернуть выбор, чтобы кнопка «Обновить» его не сбрасывала
  fromSelect.value = fromValue;
  toSelect.value = toValue;
}

// У активной колонки стрелка ▲ или ▼, у остальных — пусто
function showSortArrows() {
  for (const button of sortButtons) {
    const arrow = button.dataset.sort === sort ? (order === 'asc' ? ' ▲' : ' ▼') : '';
    button.querySelector('.arrow').textContent = arrow;
  }
}

// Вызывается при нажатии «Конвертировать»
async function convert() {
  const amount = document.querySelector('#amount').value;
  const from = fromSelect.value;
  const to = toSelect.value;

  try {
    // URLSearchParams сам соберёт ?amount=100&from=USD&to=EUR и закодирует значения
    const response = await fetch(`/api/convert?${new URLSearchParams({ amount, from, to })}`);
    const data = await response.json();
    if (!response.ok) {
      convertResult.textContent = data.error;   // 400 / 404 / 502 — текст ошибки придумал сервер
      return;
    }
    convertResult.textContent = `${data.amount} ${data.from} = ${data.result} ${data.to}`;
  } catch {
    convertResult.textContent = 'Сервер недоступен.';
  }
}

// Подписки — один раз при загрузке страницы, а не внутри loadRates()
convertForm.addEventListener('submit', event => {
  event.preventDefault();   // без этого браузер сам отправит форму и перезагрузит страницу
  convert();
});
refreshButton.addEventListener('click', loadRates);

// Клик по заголовку: та же колонка — сменить направление, новая — по возрастанию
for (const button of sortButtons) {
  button.addEventListener('click', () => {
    if (button.dataset.sort === sort) {
      order = order === 'asc' ? 'desc' : 'asc';
    } else {
      sort = button.dataset.sort;
      order = 'asc';
    }
    loadRates();
  });
}

loadRates();
