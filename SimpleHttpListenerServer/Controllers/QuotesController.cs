using SimpleHttpListenerServer.Core;

namespace SimpleHttpListenerServer.Controllers;

[Authorize(AllowAnonymous = true)]
public sealed class QuotesController(QuotesApiClient quotes) : BaseController
{
    [HttpGet("/api/quotes/random")]
    public async Task<IActionResult> GetRandomQuotes()
    {
        var quote = await quotes.GetRandomQuoteAsync(HttpContext.RequestAborted);
        return Ok(quote);
    }
}