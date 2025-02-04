using API.Helpers;
using API.Models.Bussiness;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services
{
    public interface ITokenService
    {
        string CreateToken(int businessId);
        bool RemoveToken(string token);
        bool IsAuthorized(string token);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string accesstoken);
    }

    public class TokenService : ITokenService
    {
        private readonly AppSettings _appSettings;
        private readonly Dictionary<string, string> _refreshTokens;

        public TokenService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _refreshTokens = new Dictionary<string, string>();
        }

        public string CreateToken(int businessId)
        {
            var signInCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Secret)),
                SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "Booksy"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("businessId", businessId.ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _appSettings.Issuer,
                audience: _appSettings.Audience,
                claims,
                null,
                DateTime.UtcNow.AddDays(1),
                signInCredentials);

            string accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return accessTokenString;
        }

        public bool RemoveToken(string token)
        {
            if (token == null)
                return false;

            return _refreshTokens.Remove(token);
        }

        public bool IsAuthorized(string token)
        {
            if (token == null)
                return false;

            return _refreshTokens.ContainsKey(token);
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string accesstoken)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Secret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(accesstoken, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }
    }
}
