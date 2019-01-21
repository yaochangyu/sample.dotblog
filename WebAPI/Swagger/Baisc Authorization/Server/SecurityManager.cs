using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Server
{
    public class SecurityManager
    {
        public static Task<bool> CheckUserAsync(string name, string password,CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //check database
            return Task.FromResult(true);
        }
    }
}