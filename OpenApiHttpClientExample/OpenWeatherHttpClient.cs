namespace OpenApiHttpClientExample;

public class OpenWeatherHttpClient
{
        private readonly HttpClient _httpClient;
        private const string ApiKey = "1dcbbabaaf365ab69e9274fdfc9d6ab7";

        public OpenWeatherHttpClient()
        {
                _httpClient = new HttpClient
                {
                        BaseAddress = new Uri("https://api.openweathermap.org/")
                };
        }

        public async Task<string?> GetWeatherDataByCity(string city)
        {
                var result = await _httpClient.GetAsync($"data/2.5/weather?appid={ApiKey}&q={city}");
                if (result.IsSuccessStatusCode)
                {
                        return await result.Content.ReadAsStringAsync();
                }
                return null;
        }
}