using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Maroik.FileStorage.Infrastructure;

public interface IJwtAuthManager
{
    void RemoveExpiredRefreshTokens(DateTime now);
}

public class JwtAuthManager(JwtTokenConfig jwtTokenConfig) : IJwtAuthManager
{
    public IImmutableDictionary<string, RefreshToken> UsersRefreshTokensReadOnlyDictionary => _usersRefreshTokens.ToImmutableDictionary();
    private readonly ConcurrentDictionary<string, RefreshToken> _usersRefreshTokens = new();  // can store in a database or a distributed cache
    private readonly byte[] _secret = Encoding.ASCII.GetBytes(jwtTokenConfig.Secret);

    // optional: clean up expired refresh tokens
    public void RemoveExpiredRefreshTokens(DateTime now)
    {
        var expiredTokens = _usersRefreshTokens.Where(x => x.Value.ExpireAt < now).ToList();
        foreach (var expiredToken in expiredTokens)
        {
            _ = _usersRefreshTokens.TryRemove(expiredToken.Key, out _);
        }
    }

    // can be more specific to ip, user agent, device name, etc.
    public void RemoveRefreshTokenByUserName(string userName)
    {
        var refreshTokens = _usersRefreshTokens.Where(x => x.Value.UserName == userName).ToList();
        foreach (var refreshToken in refreshTokens)
        {
            _ = _usersRefreshTokens.TryRemove(refreshToken.Key, out _);
        }
    }

    public JwtAuthResult GenerateTokens(string username, Claim[] claims, DateTime now)
    {
        var shouldAddAudienceClaim = string.IsNullOrWhiteSpace(claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Aud)?.Value);
        JwtSecurityToken jwtToken = new(
            jwtTokenConfig.Issuer,
            shouldAddAudienceClaim ? jwtTokenConfig.Audience : string.Empty,
            claims,
            expires: now.AddMinutes(jwtTokenConfig.AccessTokenExpiration),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_secret), SecurityAlgorithms.HmacSha256Signature));
        new JwtSecurityTokenHandler().WriteToken(jwtToken);

        RefreshToken refreshToken = new()
        {
            UserName = username,
            TokenString = GenerateRefreshTokenString(),
            ExpireAt = now.AddMinutes(jwtTokenConfig.RefreshTokenExpiration)
        };
        _ = _usersRefreshTokens.AddOrUpdate(refreshToken.TokenString, refreshToken, (_, _) => refreshToken);

        return null;
    }

    public JwtAuthResult Refresh(string refreshToken, string accessToken, DateTime now)
    {
        var (principal, jwtToken) = DecodeJwtToken(accessToken);
        if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature))
        {
            throw new SecurityTokenException("Invalid token");
        }

        var userName = principal.Identity?.Name;
        if (!_usersRefreshTokens.TryGetValue(refreshToken, out var existingRefreshToken) || existingRefreshToken.UserName != userName || existingRefreshToken.ExpireAt < now)
        {
            throw new SecurityTokenException("Invalid token");
        }

        return GenerateTokens(userName, principal.Claims.ToArray(), now); // need to recover the original claims
    }

    public (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException("Invalid token");
        }
        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtTokenConfig.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_secret),
                    ValidAudience = jwtTokenConfig.Audience,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                },
                out var validatedToken);
        return (principal, validatedToken as JwtSecurityToken);
    }

    private static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[32];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

public abstract class JwtAuthResult;

public class RefreshToken
{
    [JsonPropertyName("username")]
    public string UserName { get; init; }    // can be used for usage tracking
    // can optionally include other metadata, such as user agent, ip address, device name, and so on

    [JsonPropertyName("tokenString")]
    public string TokenString { get; init; }

    [JsonPropertyName("expireAt")]
    public DateTime ExpireAt { get; init; }
}