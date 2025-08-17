using System;
using System.Collections.Generic;
using System.Web;

namespace AspNetFx.WebApi
{
    public class HttpContextProvider
    {
        private HttpContext Context => HttpContext.Current;

        public Dictionary<string, object> GetServerVariables()
        {
            var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            if (Context?.Request?.ServerVariables != null)
            {
                foreach (string key in Context.Request.ServerVariables.AllKeys)
                {
                    variables[key] = Context.Request.ServerVariables[key] ?? "";
                }
            }

            return variables;
        }
    }
}