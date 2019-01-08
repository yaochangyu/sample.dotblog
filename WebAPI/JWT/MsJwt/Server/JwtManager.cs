using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Server
{
    public class JwtManager
    {
        /// <summary>
        ///     Use the below code to generate symmetric Secret Key
        ///     var hmac = new HMACSHA256();
        ///     var key = Convert.ToBase64String(hmac.Key);
        /// </summary>
        private const string Secret =
            "db3OIsj+BXE9NZDy0t8W3TcNekrF+2d/1sFnWG4HnV8TZY30iTOdtVWJG8abWvB1GlOgJuQZdcF2Luqm/hccMw==";

        private static DateTime? s_now;

        internal static DateTime? Now
        {
            get
            {
                if (s_now.HasValue == false)
                {
                    return DateTime.UtcNow;
                }

                return s_now;
            }
            set { s_now = value; }
        }

        public static string GenerateToken(string userName, int expireMinutes = 20)
        {
            var secretBytes = Convert.FromBase64String(Secret);
            var securityKey = new SymmetricSecurityKey(secretBytes);
            var handler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, userName)
                }),
                NotBefore = Now.Value,
                Expires = Now.Value.AddMinutes(expireMinutes),

                SigningCredentials = new SigningCredentials(securityKey,
                                                            SecurityAlgorithms.HmacSha256Signature)
            };

            var securityToken = handler.CreateToken(tokenDescriptor);
            var token = handler.WriteToken(securityToken);

            return token;
        }

        public static bool TryValidateToken(string token, out ClaimsPrincipal principal)
        {
            principal = null;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwt = handler.ReadJwtToken(token);

                if (jwt == null)
                {
                    return false;
                }

                var secretBytes = Convert.FromBase64String(Secret);

                var validationParameters = new TokenValidationParameters
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(secretBytes),

                    //LifetimeValidator = LifetimeValidator

                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken securityToken;
                principal = handler.ValidateToken(token, validationParameters, out securityToken);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool LifetimeValidator(DateTime? notBefore,
                                              DateTime? expires,
                                              SecurityToken securityToken,
                                              TokenValidationParameters validationParameters)
        {
            if (expires != null)
            {
                if (Now.Value < expires)
                {
                    return true;
                }
            }

            return false;
        }
    }
}