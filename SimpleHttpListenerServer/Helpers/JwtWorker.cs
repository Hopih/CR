using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SimpleHttpListenerServer.Entities;

namespace SimpleHttpListenerServer.Helpers;

public static class JwtWorker
{
    public static string SecretKey { get; set; } = null!;
    public static int TimeToLeave { get; set; }
    public static string Audience { get; set; } = null!;
    public static string Issuer { get; set; } = null!;
    
    public static string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("Role", user.Role!)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,           
            expires: DateTime.UtcNow.AddMinutes(TimeToLeave),
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
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
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
            return (false, null);
        }
    }
}