using System.Collections.Generic;
using Nancy.Security;

namespace Lab.Security.BasicAuthentication
{
    public class UserIdentity : IUserIdentity
    {
        public string UserName { get; set; }

        public IEnumerable<string> Claims { get; set; }
    }
}