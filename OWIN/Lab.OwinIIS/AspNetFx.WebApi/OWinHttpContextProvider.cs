using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;

namespace AspNetFx.WebApi
{
    public class OWinHttpContextProvider : IHttpContextProvider
    {
        private IOwinContext Context
        {
            get
            {
                var httpContext = HttpContext.Current;
                if (httpContext?.Items["owin.Environment"] != null)
                {
                    return new OwinContext((IDictionary<string, object>)httpContext.Items["owin.Environment"]);
                }
                return null;
            }
        }

        public Dictionary<string, object> GetServerVariables()
        {
            var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var context = Context;
            
            if (context?.Environment != null)
            {
                foreach (var kvp in context.Environment)
                {
                    variables[kvp.Key] = kvp.Value ?? "";
                }
            }

            return variables;
        }

        /// <summary>取得查詢字串值</summary>
        public string GetQueryString(string name)
        {
            var context = Context;
            if (context?.Request?.Query != null)
            {
                return context.Request.Query[name];
            }
            return null;
        }

        /// <summary>取得請求標頭值</summary>
        public string[] GetHeader(string name)
        {
            var context = Context;
            if (context?.Request?.Headers != null)
            {
                var headerValues = context.Request.Headers.GetValues(name);
                return headerValues?.ToArray();
            }
            return null;
        }
    }
}