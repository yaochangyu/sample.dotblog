namespace Lab.MsDI.Lazy;

public interface IService
{
    string Get();
}

public class Service : IService
{
    private readonly IServiceA _serviceA;
    private readonly IServiceB _serviceB;

    public Service(IServiceA serviceA,
        IServiceB serviceB)
    {
        this._serviceA = serviceA;
        this._serviceB = serviceB;
    }

    public string Get()
    {
        var random = new Random().Next(1, 10);
        if (random % 2 == 0)
        {
            return this._serviceB.Get();
        }

        return this._serviceA.Get();
    }
}

public interface IServiceA
{
    string Get();
}

public class ServiceA : IServiceA
{
    public ServiceA() => Console.WriteLine("Create instance for ServiceA");

    public string Get() => "ServiceA";
}

public interface IServiceB
{
    string Get();
}

public class ServiceB : IServiceB
{
    public ServiceB() => Console.WriteLine("Create instance for ServiceB");

    public string Get() => "ServiceB";
}