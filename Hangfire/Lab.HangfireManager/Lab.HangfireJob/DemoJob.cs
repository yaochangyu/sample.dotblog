using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;

namespace Lab.HangfireJob
{
    [ManagementPage("演示")]
    public class DemoJob
    {
        [Hangfire.Dashboard.Management.Support.Job]
        [DisplayName("呼叫內部方法")]
        [Description("呼叫內部方法")]
        [AutomaticRetry(Attempts = 3)]   //自動重試
        [DisableConcurrentExecution(90)] //禁止使用並行
        public void Action(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            if (cancellationToken.ShutdownToken.IsCancellationRequested)
            {
                return;
            }

            context.WriteLine($"測試用，Now:{DateTime.Now}");
            Thread.Sleep(30000);
        }

        [Queue("WebAPI")]
        [Hangfire.Dashboard.Management.Support.Job]
        [DisplayName("呼叫外部服務")]
        [Description("呼叫外部服務")]
        [AutomaticRetry(Attempts = 3)]   //自動重試
        [DisableConcurrentExecution(90)] //禁止使用並行
        public string CallWebApi(
            [DisplayData("服務端點",
                         "http://localhost:8080/test.html?name=youname",
                         "請求位置必須為 http or https")]
            Uri url,
            [DisplayData("請求方式",
                         "POST",
                         "常用的請求方式 GET、POST、PUT、DELETE ")]
            HttpMethod method = HttpMethod.POST,
            [DisplayData("請求內容", IsMultiLine = true)]
            string content = null,
            [DisplayData("內容類型",
                         "application/x-www-form-urlencoded",
                         "常見的有 application/x-www-form-urlencoded、multipart/form-data、text/plain、application/json 等",
                         "application/x-www-form-urlencoded")]
            string contentType = "application/x-www-form-urlencoded",
            [DisplayData("內容編碼方式",
                         "utf-8",
                         "常見的有 UTF-8、Unicode、ASCII，錯誤的編碼會導致內容無法正確呈現",
                         "utf-8",
                         ConvertType = typeof(EncodingsInputDataList))]
            string contentEncoding = "UTF-8",
            [DisplayData("回應結果內容編碼",
                         "utf-8",
                         "常見的有 UTF-8、Unicode、ASCII，錯誤的編碼會導致內容無法正確呈現",
                         "utf-8",
                         ConvertType = typeof(EncodingsInputDataList))]
            string responseEncoding = "UTF-8",
            PerformContext context = null)
        {
            var defaultContentEncoding  = Encoding.GetEncoding(contentEncoding)  ?? Encoding.UTF8;
            var defaultResponseEncoding = Encoding.GetEncoding(responseEncoding) ?? Encoding.UTF8;

            context.WriteLine($"測試用，Now:{DateTime.Now}");

            //Thread.Sleep(30000);
            return $"模擬呼叫Web API返回結果，目前時間：{DateTime.Now}，內容編碼：{defaultContentEncoding}，回傳編碼：{defaultResponseEncoding}";
        }
    }
}