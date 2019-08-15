using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Lab.Compress.Filters
{
    public class DeflateDecompressionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var content           = actionContext.Request.Content;
            var zipContentBytes   = content         == null ? null : content.ReadAsByteArrayAsync().Result;
            var unzipContentBytes = zipContentBytes == null ? new byte[0] : Deflate.Decompress(zipContentBytes);
            actionContext.Request.Content = new ByteArrayContent(unzipContentBytes);
            base.OnActionExecuting(actionContext);
        }
    }
}