using System.Collections.Generic;
using Nancy.Authentication.Basic;
using Nancy.Security;

namespace Lab.Security.BasicAuthentication
{
    public class UserValidator : IUserValidator
    {
        public IUserIdentity Validate(string username, string password)
        {
            if (username == "yao" && password == "pass@w0rd1~")
            {
                var identity = new UserIdentity
                {
                    UserName = username,
                    Claims   = new List<string> {"User"}
                };
                return identity;
            }

            //anonymous.

            return null;
        }
    }
}