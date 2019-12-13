using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TPL_IProgress
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private CancellationTokenSource m_cts = new CancellationTokenSource();

        private async void button_AsyncStart_Click(object sender, EventArgs e)
        {
            if (this.m_cts.IsCancellationRequested)
            {
                return;
            }

            label1.Text = "";
            label2.Text = "";

            var progress = new Progress<ProgressInfo>();

            progress.ProgressChanged += (o, info) =>
            {
                label1.Text = info.Data.ToString();
            };

            var maskTask = await Task.Factory.StartNew(() => SumAsync(this.m_cts.Token, progress)); var status = string.Format("任務完成，完成狀態為：\r\nIsCanceled={0}\r\nIsCompleted={1}\r\nIsFaulted={2}\r\n",
                      maskTask.IsCanceled,
                      maskTask.IsCompleted,
                      maskTask.IsFaulted);

            if (!maskTask.IsCanceled && !maskTask.IsFaulted)
            {
                status += string.Format("\r\n計算結果:{0}", maskTask.Result);
            }
            label2.Text = status.ToString();
        }

        private async Task<long> SumAsync(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
        {
            long sum = 0;
            var info = new ProgressInfo();
            for (int i = 0; i < 1000; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                sum++;

                if (progress != null)
                {
                    info.Data = sum;
                    progress.Report(info);
                }

                SpinWait.SpinUntil(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return false;
                }, 10);
            }
            return sum;
        }

        private Task<long> SumAsync1(CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
        {
            var task = Task.Factory.StartNew(() =>
            {
                long sum = 0;
                var info = new ProgressInfo();
                for (int i = 0; i < 100; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    sum++;
                    if (progress != null)
                    {
                        info.Data = sum;
                        progress.Report(info);
                    }
                    SpinWait.SpinUntil(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        return false;
                    }, 10);
                }
                return sum;
            });
            return task;
        }

        private void button_AsyncStop_Click(object sender, EventArgs e)
        {
            if (this.m_cts.IsCancellationRequested)
            {
                return;
            }
            this.m_cts.Cancel();
            m_cts = new CancellationTokenSource();
        }

        private void button_SyncStart_Click(object sender, EventArgs e)
        {
            Sum();
        }

        private long Sum()
        {
            long sum = 0;
            var info = new ProgressInfo();
            for (int i = 0; i < 1000; i++)
            {
                sum++;
                SpinWait.SpinUntil(() =>
                {
                    return false;
                }, 10);
            }
            return sum;
        }
    }

    internal class ProgressInfo
    {
        public long Data { get; internal set; }
    }
}