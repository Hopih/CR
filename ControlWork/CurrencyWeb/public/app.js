const refreshButton = document.querySelector('#refresh');
const statusElement = document.querySelector('#status');
const ratesBody = document.querySelector('#rates');
const dateElement = document.querySelector('#rates-date');
const numberFormat = new Intl.NumberFormat('ru-RU', { minimumFractionDigits: 4, maximumFractionDigits: 4 });

async function loadRates() {
  refreshButton.disabled = true;
  statusElement.textContent = 'Загрузка…';
  try {
    const response = await fetch('/api/rates');
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
    const date = new Date(data.date).toLocaleDateString('ru-RU', { timeZone: 'Europe/Moscow' });
    dateElement.textContent = `Курсы на ${date}`;
    statusElement.textContent = `Загружено валют: ${rows.length}`;
  } catch (error) {
    statusElement.textContent = `${error.message} Если таблица уже была загружена, в ней остались прежние данные.`;
  } finally {
    refreshButton.disabled = false;
  }
}

refreshButton.addEventListener('click', loadRates);
loadRates();
