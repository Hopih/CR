using System.Net.Http.Json;

namespace CurrencyWeb;

public sealed class RatesService(HttpClient httpClient)
{
    private const string ApiUrl = "https://www.cbr-xml-daily.ru/daily_json.js";
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private RatesResponse? _cachedRates;
    private DateTimeOffset _cacheExpiresAt;
    private DateTimeOffset _nextRequestAt;

    public async Task<RatesResponse> GetRatesAsync()
    {
        // Кеш общий для всех посетителей этого сервера.
        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedRates is not null && DateTimeOffset.UtcNow < _cacheExpiresAt)
                return _cachedRates;
            if (DateTimeOffset.UtcNow < _nextRequestAt)
                throw new HttpRequestException("Источник временно недоступен.");

            // Даже неудачные запросы не повторяем чаще одного раза в минуту.
            _nextRequestAt = DateTimeOffset.UtcNow.AddMinutes(1);
            var data = await httpClient.GetFromJsonAsync<ProviderRates>(ApiUrl)
                ?? throw new HttpRequestException("Источник вернул пустой ответ.");

            if (data.Valute.Count == 0 || data.Valute.Values.Any(c => c.Nominal <= 0 || c.Value <= 0))
                throw new HttpRequestException("Источник вернул некорректные курсы.");

            _cachedRates = new RatesResponse(data.Date, data.Valute.Values.ToArray());
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            return _cachedRates;
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}

public sealed record CurrencyRate(string CharCode, string Name, int Nominal, decimal Value);
public sealed record RatesResponse(DateTimeOffset Date, CurrencyRate[] Currencies);
public sealed record ProviderRates(DateTimeOffset Date, Dictionary<string, CurrencyRate> Valute);
