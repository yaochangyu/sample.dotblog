using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;

namespace AspNetFx.WebApi
{
    public class OWinHttpContextProvider : IHttpContextProvider
    {
        private readonly IOwinContext _context;

        public OWinHttpContextProvider(IOwinContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private IOwinContext Context
        {
            get
            {
                // 先嘗試從注入的 context 取得
                if (_context != null)
                {
                    return _context;
                }

                // 如果沒有注入的 context，再嘗試從 HttpContext.Current 取得（相容性考量）
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
