using System.Web.Http;

namespace Server.Controllers.V1
{
    /// <summary>
    ///     第一版
    /// </summary>
    [VersionRoute("api/version", 1)]
    public class ValuesController : ApiController
    {
        /// <summary>
        /// 查詢
        /// </summary>
        /// <response code="400"/>
        /// <returns></returns>
        public IHttpActionResult Get()
        {
            return this.BadRequest("我是地板");
        }
    }
}