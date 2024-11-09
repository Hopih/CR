using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleHttpListenerServer;

public class QuotesApiClient
{
    private static HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://quotes15.p.rapidapi.com"),
        DefaultRequestHeaders =
        {
            {
                "x-rapidapi-host", "quotes15.p.rapidapi.com"
            },
            {
                "x-rapidapi-key", "11c5aa0932msh6f977a8bfc17035p154120jsn86e885da8be0"
            }
        }
    };


    public async Task<QuotesApiResponse> GetRandomQuoteAsync(CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<QuotesApiResponse>("/quotes/random/?language_code=ru", 
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }, cancellationToken: cancellationToken);
        if (result == null)
        {
            throw new ApplicationException("No quotes available");
        }

        return result;
    }
}