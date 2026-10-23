using HttpListenerStepByStep.Auth;
using HttpListenerStepByStep.Data;
using HttpListenerStepByStep.Http;
using HttpListenerStepByStep.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace HttpListenerStepByStep;

public static class RouterExtensions 
{
    public static Router CreateRouterAndMap(QuotesApiClient quotes)
    {
        var connection = Environment.GetEnvironmentVariable("BOARD_DB_CONNECTION")
                         ?? "Host=localhost;Port=5432;Database=my_miro;Username=postgres;Password=postgres";

        var userRepository = new UserRepository(connection);

        var registration = new RegistrationHandler(
            userRepository,
            new RegisterValidator(), 
            new PasswordHasher<object>());
        
        var sessionStore = new SessionStore(new MemoryCache(new MemoryDistributedCacheOptions()));
        var sessionCookie = new SessionCookie(false);
        
        var auth = new AuthService(userRepository, sessionStore, sessionCookie);
        
        var boards = new BoardApiHandler(new BoardRepository(connection),
            new StickerRepository(connection), 
            new CreateBoardValidator(), 
            new SaveStickerValidator(),
            new AuthService(userRepository, sessionStore, sessionCookie),
            new BoardOwnerRepository(connection));
        
        var authHandler = new AuthHandler(userRepository, sessionStore, sessionCookie, auth,
            new LoginValidator(), new PasswordHasher<object>());

        var router = new Router();

        router.Map("POST", "/api/auth/login", authHandler.LoginAsync);
        router.Map("GET", "/api/auth/me", authHandler.MeAsync);
        router.Map("POST", "/api/auth/logout", authHandler.LogoutAsync);
        router.Map("GET", "/api/boards", boards.ListAsync);
        router.Map("POST", "/api/users/register", registration.RegisterAsync);
        router.Map("POST", "/api/boards", boards.CreateAsync);
        router.Map("GET", "/api/boards/{boardId}", boards.GetAsync);
        router.Map("DELETE", "/api/boards/{boardId}", boards.DeleteAsync);
        router.Map("POST", "/api/boards/{boardId}/stickers", boards.CreateStickerAsync);
        router.Map("PUT", "/api/boards/{boardId}/stickers/{stickerId}", boards.UpdateStickerAsync);
        router.Map("DELETE", "/api/boards/{boardId}/stickers/{stickerId}", boards.DeleteStickerAsync);
        router.Map("GET", "/quote/random", async (route, ct) =>
            await route.Http.Response.WriteAsync(200, await quotes.GetRandomQuoteAsync(ct), ct));
        
        return router;
    }
}