using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;

namespace Server.Controllers
{
    public class TokenController : ApiController
    {
        // POST api/token
        [AllowAnonymous]
        public IHttpActionResult Post(LoginData loginData)
        {
            if (this.CheckUser(loginData.UserName, loginData.Password))
            {
                var token = JwtManager.GenerateToken(loginData.UserName);

                return new ResponseMessageResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(token, Encoding.UTF8)
                });
            }

            throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }

         bool CheckUser(string username, string password)
        {
            // should check in the database
            return true;
        }

        public class LoginData
        {
            public string UserName { get; set; }

            public string Password { get; set; }
        }
    }
}