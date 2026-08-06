using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace FixMyCity.Infrastructure
{
    //   - Access token  = short-lived signed JWT, carries RoleId/ConsumerId/Email as claims
    //   - Refresh token = opaque random value; only its SHA-256 hash is ever stored in the DB
    //
    // Web.config keys :
    //   <add key="JwtSecretKey" " />
    //   <add key="JwtExpiryMinutes" value="15" />
    //   <add key="JwtRefreshExpiryDays" value="7" />
    public static class JwtHelper
    {
        private static readonly string SecretKey =
            ConfigurationManager.AppSettings["JwtSecretKey"];

        static JwtHelper()
        {
            if (string.IsNullOrEmpty(SecretKey))
            {
                throw new InvalidOperationException(
                    "JwtSecretKey missing from Web.config");
            }
        }

        private static readonly int ExpiryMinutes = GetConfigInt("JwtExpiryMinutes", 15);
        private static readonly int RefreshExpiryDays = GetConfigInt("JwtRefreshExpiryDays", 7);

        // ── Access token (JWT, short-lived) ─────────────────────────────
        public static string GenerateToken(int consumerId, int roleId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("ConsumerId", consumerId.ToString()),
                new Claim("RoleId", roleId.ToString()),
                new Claim("Email", email)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validated;
                return new JwtSecurityTokenHandler()
                           .ValidateToken(token, parameters, out validated);
            }
            catch
            {
                return null; // expired, tampered, or garbage — all treated the same
            }
        }

        // ── Refresh token (opaque random, long-lived) ───────────────────
        public static string GenerateRefreshToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[64];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        public static string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                return Convert.ToBase64String(sha256.ComputeHash(bytes));
            }
        }

        // ── Read access token from cookie ───────────────────────────────
        public static string GetTokenFromRequest()
        {
            var cookie = HttpContext.Current.Request.Cookies["jwt_token"];
            return cookie != null ? cookie.Value : null;
        }

        public static int GetRefreshExpiryDays() => RefreshExpiryDays;
        public static int GetAccessExpiryMinutes() => ExpiryMinutes;

        private static int GetConfigInt(string key, int defaultValue)
        {
            var val = ConfigurationManager.AppSettings[key];
            if (val == null) return defaultValue;

            int parsed;
            if (int.TryParse(val, out parsed)) return parsed;

            throw new InvalidOperationException(
                string.Format("Web.config key '{0}' must be an integer.", key));
        }
    }
}