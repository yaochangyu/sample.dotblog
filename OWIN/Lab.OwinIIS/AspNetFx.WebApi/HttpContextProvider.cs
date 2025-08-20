using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Web;

namespace AspNetFx.WebApi
{
    public class HttpContextProvider : IHttpContextProvider
    {
        private readonly HttpContextBase _context;

        public HttpContextProvider(HttpContextBase context)
        {
            this._context = context;
        }

        public Dictionary<string, object> GetServerVariables()
        {
            var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (_context?.Request?.ServerVariables != null)
            {
                foreach (string key in _context.Request.ServerVariables.AllKeys)
                {
                    variables[key] = _context.Request.ServerVariables[key] ?? "";
                }
            }

            return variables;
        }


        /// <summary>取得查詢字串值</summary>
        public string GetQueryString(string name)
        {
            return _context.Request.QueryString[name];
        }

        /// <summary>取得請求標頭值</summary>
        public string[] GetHeader(string name)
        {
            return _context.Request.Headers.GetValues(name);
        }
    }
}
