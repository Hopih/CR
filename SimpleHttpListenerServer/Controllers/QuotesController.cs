using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class QuotesController(QuotesApiClient quotes)
{
    [HttpGet("/api/quotes/random")]
    public async Task<object> GetRandomQuotes(HttpContextEx ctx)
    {
        var quote = await quotes.GetRandomQuoteAsync(ctx.RequestAborted);
        return quote;
    }
}