using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class QuotesController(QuotesApiClient quotes)
{
    [HttpGet("/api/quotes/random")]
    public async Task<object> GetRandomQuotes(CancellationToken cancel = default)
    {
        var quote = await quotes.GetRandomQuoteAsync(cancel);
        return quote;
    }
}