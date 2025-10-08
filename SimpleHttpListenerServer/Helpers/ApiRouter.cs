using System.Net;
using System.Text.RegularExpressions;
using Npgsql;
using SimpleHttpListenerServer.Database;
using SimpleHttpListenerServer.Entities;
using SimpleHttpListenerServer.Models;

namespace SimpleHttpListenerServer.Helpers;

public record Route(string Method, string Pattern, Func<HttpListenerContext, Match, CancellationToken, Task> Handler);


public sealed class ApiRouter
{
    private readonly List<Route> _routes = new();
    private QuotesApiClient quotesApiClient = new QuotesApiClient();
    private NpgsqlDataSource dataSource;

    public ApiRouter(NpgsqlDataSource dataSource)
    {
        _routes.Add(new("POST", "^/api/user/signup$", Signup));
        _routes.Add(new("POST", "^/api/user/login$", Login));
        _routes.Add(new("GET", "^/api/boards$", GetUserBoards));
        _routes.Add(new("POST", "^/api/boards$", CreateUserBoard));
        _routes.Add(new("POST", "^/api/guest/board$", CreateGuestBoard));
        _routes.Add(new("GET", "^/api/quotes/random$", GetRandomQuotes));
        _routes.Add(new("GET", "^/api/boards/([0-9a-fA-F-]+)/elements$", GetBoardElements));
        _routes.Add(new("POST", "^/api/boards/([0-9a-fA-F-]+)/elements/bulk$", UpsertBoardElements));
        
        this.dataSource = dataSource;
    }
    
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        var method = ctx.Request.HttpMethod;

        try
        {
            foreach (var route in _routes)
            {
                if (route.Method != method) continue;
                var match = Regex.Match(path, route.Pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    await route.Handler(ctx, match, ct);
                    return;
                }
            }

            await ctx.Response.WriteErrorAsync(404, "not_found", ct);
        }
        catch (Exception ex)
        {
            await ctx.Response.WriteErrorAsync(500, ex.Message, ct);
        }
    }
    

    private async Task Signup(HttpListenerContext ctx, Match _,  CancellationToken ct)
    {
        var dto = await ctx.Request.ReadJsonAsync<SignupRequest>(ct);
        if (dto is null )
        {
            await ctx.Response.WriteErrorAsync(400, "login_and_password_required", ct);
            return;
        }
        
        var validator = new UserValidationRules();
        var result = await validator.ValidateAsync(dto, ct);

        if (!result.IsValid)
        {
            await ctx.Response.WriteJsonAsync( new {
                errors = result.Errors.Select(e => e.ErrorMessage).ToList()
            }, 400, ct);
            return;
        }

        var repo = new UsersRepository(dataSource);
        try
        {
            var userId = await repo.CreateUserAsync(dto.Login, dto.Password, ct);
            if (userId is null)
            {
                await ctx.Response.WriteErrorAsync(500, "signup_failed", ct);
                return;
            }
            await ctx.Response.WriteJsonAsync(new { id = userId }, 201, ct);
        }
        catch (PostgresException pg) when (pg.SqlState == "23505")
        {
            await ctx.Response.WriteErrorAsync(409, "login_already_exists", ct);
        }
    }

    private async Task GetRandomQuotes(HttpListenerContext ctx, Match _, CancellationToken ct)
    {
        var quote = await quotesApiClient.GetRandomQuoteAsync(ct);
        await ctx.Response.WriteJsonAsync(quote, 200, ct);
    }

    private async Task Login(HttpListenerContext ctx, Match _, CancellationToken ct)
    {
        var dto = await ctx.Request.ReadJsonAsync<LoginRequest>(ct);
        if (dto is null) { await ctx.Response.WriteErrorAsync(400, "bad_request", ct); return; }

        var repo = new UsersRepository(dataSource);
        var row = await repo.GetByLoginAsync(dto.Login, ct);
        if (row is null || !MyPasswordHasher.Validate($"{row.Hash};{row.Salt}", dto.Password))
        {
            await ctx.Response.WriteErrorAsync( 401, "invalid_credentials", ct);
            return;
        }

        var token = JwtWorker.GenerateJwtToken(new User { Id = row.Id, Role = row.Role });
        await ctx.Response.WriteJsonAsync(new LoginResponse { Token = token, UserId = row.Id, Role = row.Role }, 200, ct);
    }

    private async Task GetUserBoards(HttpListenerContext ctx, Match _, CancellationToken ct)
    {
        var user = ctx.Request.GetUser();
        if (user is null) { await ctx.Response.WriteErrorAsync(401, "unauthorized", ct); return; }

        var repo = new BoardsRepository(dataSource);
        var boards = await repo.GetUserBoardsAsync(user.Id, ct);
        await ctx.Response.WriteJsonAsync(boards.Select(b => new { b.Id, b.Title }).ToList(), 200, ct);
    }

    private async Task CreateUserBoard(HttpListenerContext ctx, Match _, CancellationToken ct)
    {
        var user = ctx.Request.GetUser();
        if (user is null) { await ctx.Response.WriteErrorAsync( 401, "unauthorized", ct); return; }

        var dto = await ctx.Request.ReadJsonAsync<CreateBoardRequest>(ct) ?? new CreateBoardRequest();
        var repo = new BoardsRepository(dataSource);
        var id = await repo.CreateUserBoardAsync(user.Id, dto.Title, ct);
        await ctx.Response.WriteJsonAsync( new { id }, 201, ct);
    }

    private async Task CreateGuestBoard(HttpListenerContext ctx, Match _, CancellationToken ct)
    {
        var dto = await ctx.Request.ReadJsonAsync<CreateBoardRequest>(ct) ?? new CreateBoardRequest { Title = "Guest board" };
        var repo = new BoardsRepository(dataSource);
        var (boardId, tempKey, expires) = await repo.CreateGuestBoardAsync(dto.Title, TimeSpan.FromDays(3), ct);
        
        ctx.Response.AddHeader("Set-Cookie", $"guest_board_key={tempKey}; Path=/; Max-Age={3*24*3600}; HttpOnly");
        await ctx.Response.WriteJsonAsync(new CreateGuestBoardResponse { BoardId = boardId, TempKey = tempKey, ExpiresAt = expires }, 201, ct);
    }

    private async Task<bool> CanAccessBoard(Guid boardId, User? user, string? guestKey, CancellationToken ct)
    {
        var boards = new BoardsRepository(dataSource);
        var meta = await boards.GetBoardMetaAsync(boardId, ct);
        if (meta is null) return false;

        // владелец
        if (user?.Id != null && meta.Value.OwnerUserId == user.Id) return true;

        // гостевой доступ
        if (meta.Value is not { IsTemporary: true, TempKey: { } tk, TempExpiresAt: { } exp }) return false;

        if (!Guid.TryParse(guestKey, out var gk))
        {
            return false;
        }
        
        if (gk == tk && exp < DateTime.UtcNow)
        {
            await boards.DeleteExpiredGuestBoardsAsync(ct);
            return false;
        }

        return !string.IsNullOrEmpty(guestKey) 
               && gk == tk && exp > DateTime.UtcNow;
    }

    private async Task GetBoardElements(HttpListenerContext ctx, Match match, CancellationToken ct)
    {
        
        var boardId = Guid.Parse(match.Groups[1].Value);
        
        var user = ctx.Request.GetUser();
        var guestKey = ctx.Request.GetCookie("guest_board_key") ?? ctx.Request.Headers["X-Guest-Key"];

        if (!await CanAccessBoard(boardId, user, guestKey, ct))
        {
            await ctx.Response.WriteErrorAsync(403, "forbidden", ct);
            return;
        }

        var repo = new ElementsRepository(dataSource);
        var rows = await repo.GetElementsWithStickersAsync(boardId, ct);

        var list = rows.Select(r => new ElementDto {
            Id = r.Id,
            Type = r.Type,
            X = r.X, Y = r.Y, Width = r.Width, Height = r.Height,
            ZIndex = r.ZIndex, Rotation = r.Rotation,
            Text = r.Text, Color = r.Color, FontSize = r.FontSize
        }).ToList();

        await ctx.Response.WriteJsonAsync(list, 200, ct);
    }

    private async Task UpsertBoardElements(HttpListenerContext ctx, Match match, CancellationToken ct)
    {
        var boardId = Guid.Parse(match.Groups[1].Value);
        var user = ctx.Request.GetUser();
        var guestKey = ctx.Request.GetCookie("guest_board_key") ?? ctx.Request.Headers["X-Guest-Key"];

        if (!await CanAccessBoard(boardId, user, guestKey, ct))
        {
            await ctx.Response.WriteErrorAsync(403, "forbidden", ct);
            return;
        }

        var dto = await ctx.Request.ReadJsonAsync<BulkElementsRequest>(ct);
        if (dto is null || dto.Elements.Count == 0) { await ctx.Response.WriteErrorAsync(400, "no_elements", ct); return; }

        var items = dto.Elements.Select(e => new BoardElement {
            Id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
            BoardId = boardId,
            UserId = user?.Id,
            Type = e.Type,
            X = e.X, Y = e.Y, Width = e.Width, Height = e.Height,
            ZIndex = e.ZIndex, Rotation = e.Rotation,
            Text = e.Text, Color = e.Color, FontSize = e.FontSize
        }).ToList();

        var repo = new ElementsRepository(dataSource);
        await repo.UpsertElementsAsync(boardId, user?.Id, items, ct);

        await ctx.Response.WriteJsonAsync(new { updated = items.Count }, 200, ct);
    }
}