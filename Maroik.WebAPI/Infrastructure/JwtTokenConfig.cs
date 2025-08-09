using System.Text.Json.Serialization;

namespace Maroik.WebAPI.Infrastructure
{
    public class JwtTokenConfig
    {
        [JsonPropertyName("secret")]
        public string Secret { get; init; }

        [JsonPropertyName("issuer")]
        public string Issuer { get; init; }

        [JsonPropertyName("audience")]
        public string Audience { get; init; }

        [JsonPropertyName("accessTokenExpiration")]
        public int AccessTokenExpiration { get; init; }

        [JsonPropertyName("refreshTokenExpiration")]
        public int RefreshTokenExpiration { get; init; }
    }
}
