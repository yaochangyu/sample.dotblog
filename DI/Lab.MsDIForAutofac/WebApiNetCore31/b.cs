namespace WebApiNetCore31
{
    public interface ITestService
    {
        string GetDate();
    }

    public class TestService : ITestService
    {
        public string GetDate()
        {
            return "service";
        }
    }

    public class TestComponent : ITestService
    {
        public string GetDate()
        {
            return "component";
        }
    }

    public interface IServiceProvider
    {
        public void setService();
    }

    // Client class
    public class Client
    {
        private IServiceProvider _service;

        public Client(IServiceProvider service)
        {
            this._service = service;
        }
    }
}