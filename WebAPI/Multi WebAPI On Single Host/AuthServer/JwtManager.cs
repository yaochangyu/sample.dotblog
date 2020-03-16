using System;
using System.Collections.Generic;
using System.Security.Claims;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;

namespace AuthServer
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
            var symmetricKey = Convert.FromBase64String(Secret);
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
            var now = (DateTimeOffset) Now.Value;
            var expired = now.AddMinutes(expireMinutes).ToUnixTimeSeconds();
            var notBefore = now.ToUnixTimeSeconds();

            var payload = new Dictionary<string, object>
            {
                {"name", userName},
                {"exp", expired},
                {"nbf", notBefore}
            };

            var token = encoder.Encode(payload, symmetricKey);

            return token;
        }

        public static bool TryValidateToken(string token, out ClaimsPrincipal principal)
        {
            var symmetricKey = Convert.FromBase64String(Secret);
            principal = null;
            var result = false;
            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();

                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                var payload = decoder.DecodeToObject(token, symmetricKey, true);
                List<Claim> claims = new List<Claim>();
                foreach (var item in payload)
                {
                    if (item.Value == null)
                    {
                        continue;
                    }

                    var key = item.Key;
                    var value = item.Value.ToString();
                    if (key.ToLower() == "name")
                    {
                        claims.Add(new Claim(ClaimTypes.Name, value));
                    }
                    else if (key.ToLower() == "role")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, value));
                    }
                    else
                    {
                        claims.Add(new Claim(key, value));
                    }
                }

                var identity = new ClaimsIdentity(claims, "JWT");
                principal = new ClaimsPrincipal(identity);
                result = true;
            }
            catch (TokenExpiredException)
            {
                Console.WriteLine("Token has expired");
            }
            catch (SignatureVerificationException)
            {
                Console.WriteLine("Token has invalid signature");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }
    }
}