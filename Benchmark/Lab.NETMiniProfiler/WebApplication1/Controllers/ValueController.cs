using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValueController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            string url1 = string.Empty;
            string url2 = string.Empty;
            using (MiniProfiler.Current.Step("Get方法"))
            {
                using (MiniProfiler.Current.Step("准备数据"))
                {
                    using (MiniProfiler.Current.CustomTiming("SQL", "SELECT * FROM Config"))
                    {
                        // 模拟一个SQL查询
                        Thread.Sleep(500);

                        url1 = "https://www.baidu.com";
                        url2 = "https://www.sina.com.cn/";
                    }
                }


                using (MiniProfiler.Current.Step("使用从数据库中查询的数据，进行Http请求"))
                {
                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + url1))
                    {
                        var client = new WebClient();
                        var reply = client.DownloadString(url1);
                    }

                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + url2))
                    {
                        var client = new WebClient();
                        var reply = client.DownloadString(url2);
                    }
                }
            }
            return new string[] { "value1", "value2" };
        }
    }
}
