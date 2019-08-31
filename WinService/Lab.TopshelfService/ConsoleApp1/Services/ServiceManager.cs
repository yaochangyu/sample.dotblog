namespace ConsoleApp1.Services
{
    public class ServiceManager
    {
        private static ServiceContainer s_container;

        public static ServiceContainer Container
        {
            get
            {
                if (s_container == null)
                {
                    s_container = new ServiceContainer();
                }

                return s_container;
            }
            set => s_container = value;
        }

        private bool _isStart;

        public void Start()
        {
            if (this._isStart)
            {
                return;
            }

            foreach (var service in Container._serviceCaches)
            {
                ((IService) service.Value).Start();
            }

            this._isStart = true;
        }

        public void Stop()
        {
            if (!this._isStart)
            {
                return;
            }

            foreach (var service in Container._serviceCaches)
            {
                ((IService) service.Value).Stop();
            }

            this._isStart = false;
        }
    }
}