using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Helpers;

public static class JwtWorker
{
    private const string SecretKey = "a-string-secret-at-least-256-bits-long";

    public static string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("Role", user.Role!)
        };

        var token = new JwtSecurityToken(
            issuer: "yourdomain.com",
            audience: "yourdomain.com",
            claims: claims,
            notBefore: DateTime.UtcNow,           
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static (bool isSuccess, User? user) ValidateJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        tokenHandler.InboundClaimTypeMap.Clear();
        
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "yourdomain.com",
            ValidAudience = "yourdomain.com",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(2) // ⏱ допускаем разницу системных часов
        };

        try
        {
            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken _);
            var userId = claimsPrincipal.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            var roleClaim = claimsPrincipal.Claims.First(x => x.Type == "Role").Value;
            return (true, new User
            {
                Id = int.Parse(userId),
                Role = roleClaim
            });
        }
        catch
        {
            return (false, default);
        }
    }
}