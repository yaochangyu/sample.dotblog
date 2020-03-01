using System;
using System.ComponentModel;
using System.Threading;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;

namespace Lab.HangfireSqliteStorage.ConsoleApp
{
    [ManagementPage("演示","default")]
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
            //Thread.Sleep(30000);
        }
    }
}