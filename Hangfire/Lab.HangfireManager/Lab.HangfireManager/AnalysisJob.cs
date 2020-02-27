using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;

namespace Lab.HangfireManager
{
    [ManagementPage("出勤異常分析", "Default")]
    public class AnalysisJob
    {
        [Hangfire.Dashboard.Management.Support.Job]
        [DisplayName("通知使用者")]
        [Description("分析資料後，將出勤異常的人找出來並發信通知他")]
        [AutomaticRetry(Attempts = 3)]   //自動重試
        [DisableConcurrentExecution(90)] //禁止使用並行
        public void SendEmail(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            if (cancellationToken.ShutdownToken.IsCancellationRequested)
            {
                return;
            }

            context.WriteLine($"測試用，Now:{DateTime.Now}");
            Thread.Sleep(30000);
        }

        [Hangfire.Dashboard.Management.Support.Job]
        [Queue("API")]
        [DisplayName("通知使用者_請求外部連接")]
        [Description("分析資料後，將出勤異常的人找出來並發信通知他")]
        [AutomaticRetry(Attempts = 3)]   //自动重试
        [DisableConcurrentExecution(90)] //禁用并行执行
        public string SendEmail(
            [DisplayData("服務端點", "http://localhost:8080/test.html?name=youname", "請求外部服務,需用http或https")]
            Uri url,
            [DisplayData("請求方式", "POST", "常用的请求方式有 GET、POST、PUT、DELETE 等...")]
            HttpMethod httpMethod = HttpMethod.POST,
            [DisplayData("請求內容", IsMultiLine = true)]
            string content = null,
            [DisplayData("內容類型", "application/x-www-form-urlencoded",
                         "常見的有 application/x-www-form-urlencoded、multipart/form-data、text/plain、application/json 等",
                         "application/x-www-form-urlencoded")]
            string contentType = "application/x-www-form-urlencoded",
            [DisplayData("內容編碼方式",
                         "UTF-8",
                         "常見的的有 UTF-8、Unicode、ASCII，錯誤的編碼會導致內容無法正確呈現",
                         "UTF-8",
                         ConvertType = typeof(EncodingsInputDataList))]
            string contentEncoding = "UTF-8",
            [DisplayData("回應結果內容編碼",
                         "UTF-8",
                         "常見的的有 UTF-8、Unicode、ASCII，錯誤的編碼會導致內容無法正確呈現",
                         "UTF-8",
                         ConvertType = typeof(EncodingsInputDataList))]
            string responseEncoding = "UTF-8",
            PerformContext        context           = null,
            IJobCancellationToken cancellationToken = null)
        {
            var _contentEncoding  = Encoding.GetEncoding(contentEncoding)  ?? Encoding.UTF8;
            var _responseEncoding = Encoding.GetEncoding(responseEncoding) ?? Encoding.UTF8;

            return "";
        }
    }
}