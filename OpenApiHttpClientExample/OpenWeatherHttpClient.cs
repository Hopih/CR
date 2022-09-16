namespace OpenApiHttpClientExample;

public class OpenWeatherHttpClient
{
        private HttpClient _httpClient;
        private const string ApiKey = "1dcbbabaaf365ab69e9274fdfc9d6ab7";
        
        public string? GetWeatherDataByCity(string city)
        {
                _httpClient = new HttpClient()
                {
                        BaseAddress = new Uri("https://api.openweathermap.org")
                };
                
                var webRequest = new HttpRequestMessage(HttpMethod.Get, $"/data/2.5/weather?appid={ApiKey}&q={city}");
                var response = _httpClient.Send(webRequest);
                if (response.IsSuccessStatusCode)
                {

                        using var reader = new StreamReader(response.Content.ReadAsStream());
                        return reader.ReadToEnd();
                }
                return null;
        }
}