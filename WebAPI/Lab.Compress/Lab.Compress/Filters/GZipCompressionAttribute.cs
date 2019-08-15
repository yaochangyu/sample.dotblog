using System.Net.Http;
using System.Web.Http.Filters;

namespace Lab.Compress.Filters
{
    public class GZipCompressionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actContext)
        {
            var content    = actContext.Response.Content;
            var bytes      = content == null ? null : content.ReadAsByteArrayAsync().Result;
            var zipContent = bytes   == null ? new byte[0] : GZip.Compress(bytes);
            actContext.Response.Content = new ByteArrayContent(zipContent);
            actContext.Response.Content.Headers.Remove("Content-Type");
            actContext.Response.Content.Headers.Add("Content-encoding", "gzip");

            //actContext.Response.Content.Headers.Add("Content-Type",     "application/json");
            actContext.Response.Content.Headers.Add("Content-Type", "application/json;charset=utf-8");
            base.OnActionExecuted(actContext);
        }
    }
}