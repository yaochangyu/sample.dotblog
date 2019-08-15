using System.Net.Http;
using System.Web.Http.Filters;

namespace Lab.Compress.Filters
{
    public class DeflateCompressionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            var content = actionContext.Response.Content;
            var sourceBytes   = content == null ? null : content.ReadAsByteArrayAsync().Result;
            var zipContent = sourceBytes == null ? new byte[0] : Deflate.Compress(sourceBytes);
            actionContext.Response.Content = new ByteArrayContent(zipContent);
            actionContext.Response.Content.Headers.Remove("Content-Type");
            actionContext.Response.Content.Headers.Add("Content-encoding", "deflate");
            //actContext.Response.Content.Headers.Add("Content-Type",     "application/json");
            actionContext.Response.Content.Headers.Add("Content-Type", "application/json;charset=utf-8");

            base.OnActionExecuted(actionContext);
        }
    }
}