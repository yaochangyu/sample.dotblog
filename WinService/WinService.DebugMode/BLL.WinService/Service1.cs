using System.ServiceProcess;

namespace BLL.WinService
{
    public partial class Service1 : ServiceBase
    {
        private readonly BatchWork _work;

        public Service1()
        {
            this.InitializeComponent();
            if (this._work == null)
            {
                this._work = new BatchWork();
            }
        }

        protected override void OnStart(string[] args)
        {
            this._work.Start();
        }

        protected override void OnStop()
        {
            this._work.Stop();
        }
    }
}