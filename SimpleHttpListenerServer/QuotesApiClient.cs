using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleHttpListenerServer;

public class QuotesApiClient
{
    private static HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.api-ninjas.com"),
        DefaultRequestHeaders =
        {
            {"X-API-Key", "zV3MLERI3/LIKlGWPXoeKQ==w55oaGmxBTqPJ24n"},
        }
    };


    public async Task<QuotesApiResponse> GetRandomQuoteAsync(CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<List<QuotesApiResponse>>("/v1/quotes", 
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }, cancellationToken: cancellationToken);
        if (result == null)
        {
            throw new ApplicationException("No quotes available");
        }

        return result[0];
    }
}