using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace pq_chat_httpserver.Realtime.Services;

public sealed class TokenValidator
{
    private readonly IConfiguration _config;

    public TokenValidator(IConfiguration config)
    {
        _config = config;
    }

    public string ValidateAndGetUserId(string token)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");

        var tvp = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = _config["Jwt:Issuer"],
            ValidAudience = _config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, tvp, out _);

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? throw new Exception("Token missing user id claim.");
    }
}
