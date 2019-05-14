using System.Web;
using Newtonsoft.Json;

namespace WebApplication1
{
    public class HttpRequestState
    {
        private static readonly string RequestVariable = "Request_Variable";

        public virtual object GetCurrentVariable()
        {
            return HttpContext.Current.Items[RequestVariable];
        }

        public virtual string GetCurrentVariableToJson()
        {
            var requestItem = this.GetCurrentVariable();
            return requestItem == null ? "Empty" : JsonConvert.SerializeObject(requestItem);
        }

        public virtual void SetCurrentVariable(object source)
        {
            HttpContext.Current.Items[RequestVariable] = source;
        }
    }
}