using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace WebApplication1
{
    public class HttpRequestState
    {
        public virtual  object GetCurrentVariable()
        {
            return HttpContext.Current.Items["RequestVariable"];
        }
        public virtual string GetCurrentVariableToJson()
        {
            var requestItem = GetCurrentVariable();
            return requestItem == null ? "Empty" : JsonConvert.SerializeObject(requestItem);
        }

        public virtual void SetCurrentVariable(object source)
        {
            HttpContext.Current.Items["RequestVariable"] = source;
        }
    }
}