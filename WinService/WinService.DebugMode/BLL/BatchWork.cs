using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLL
{
    public class BatchWork : IDisposable
    {
        private bool _disposing;
        private volatile bool _isStop;

        public int Interval { get; set; } = 1000;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BatchWork()
        {
            this.Dispose(false);
        }

        public void Start()
        {
            Task.Factory
                .StartNew(() =>
                          {
                              while (!this._isStop)
                              {
                                  try
                                  {
                                      Console.WriteLine("Hello World~");
                                  }
                                  catch (Exception ex)
                                  {
                                      Console.WriteLine(ex.Message);
                                  }
                                  finally
                                  {
                                      //空轉
                                      SpinWait.SpinUntil(() => this._isStop, this.Interval);
                                  }
                              }
                          });
        }

        public void Stop()
        {
            this._isStop = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposing)
            {
                return;
            }

            this._disposing = true;
            if (disposing)
            {
                // Dispose managed resources.
                this._isStop = true;
            }

            // Dispose unmanaged managed resources.
        }
    }
}