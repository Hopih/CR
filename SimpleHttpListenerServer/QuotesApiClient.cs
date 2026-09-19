using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleHttpListenerServer;

public class QuotesApiClient
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.api-ninjas.com"),
        DefaultRequestHeaders =
        {
            {"X-API-Key", "zV3MLERI3/LIKlGWPXoeKQ==w55oaGmxBTqPJ24n"},
        }
    };
    
    private readonly JsonSerializerOptions _globalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    public async Task<QuotesApiResponse> GetRandomQuoteAsync(CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetFromJsonAsync<List<QuotesApiResponse>>("/v2/randomquotes", 
            _globalJsonOptions, cancellationToken: cancellationToken);
        
        return result == null 
            ? throw new ApplicationException("No quotes available") : 
            result[0];
    }
}