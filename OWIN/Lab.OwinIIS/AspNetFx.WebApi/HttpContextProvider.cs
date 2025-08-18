using System;
using System.Collections.Generic;
using System.Web;

namespace AspNetFx.WebApi
{
    public class HttpContextProvider : IHttpContextProvider
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


        /// <summary>取得查詢字串值</summary>
        public string GetQueryString(string name)
        {
            return Context.Request.QueryString[name];
        }

        /// <summary>取得請求標頭值</summary>
        public string[] GetHeader(string name)
        {
            return Context.Request.Headers.GetValues(name);
        }
    }
}
