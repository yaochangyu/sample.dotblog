using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebApiCore31
{
    public class JwtAuthenticationProvider : IJwtAuthenticationProvider
    {
        private readonly AppSettings _appSettings;

        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private readonly List<User> _fakeUsers = new List<User>
        {
            new User {Id = 1, FirstName = "Test", LastName = "User", Username = "yao", Password = "123456"}
        };

        public JwtAuthenticationProvider(IOptions<AppSettings> appSettings)
        {
            this._appSettings = appSettings.Value;
        }
        public JwtAuthenticationProvider(AppSettings appSettings)
        {
            this._appSettings = appSettings;
        }

        public string Authenticate(string userName, string password)
        {
            var user = this._fakeUsers.SingleOrDefault(x => x.Username == userName && x.Password == password);

            // return null if user not found
            if (user == null)
            {
                return null;
            }

            // authentication successful so generate jwt token
            var tokenHandler         = new JwtSecurityTokenHandler();
            var key                  = Encoding.UTF8.GetBytes(this._appSettings.Secret);
            var symmetricSecurityKey = new SymmetricSecurityKey(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID

                    //new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials =
                    new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}