using System;
using System.ComponentModel;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;

namespace Lab.HangfireUnitTest
{
    [ManagementPage("演示", "low")]
    public class DemoJob
    {
        public IBackgroundJobClient JobClient
        {
            get
            {
                if (this._jobClient == null)
                {
                    this._jobClient = new BackgroundJobClient();
                }

                return this._jobClient;
            }
            set => this._jobClient = value;
        }

        private IBackgroundJobClient _jobClient;

        public DemoJob(IBackgroundJobClient jobClient)
        {
            this.JobClient = jobClient;
        }

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

        public void EnqueueAction()
        {
            this.JobClient.Enqueue(() => this.Action(null, JobCancellationToken.Null));
        }
    }
}