namespace Lab.MsDI.Lazy;

public class ServiceLazy : IService
{
    private readonly Lazy<IServiceA> _serviceA;
    private readonly Lazy<IServiceB> _serviceB;

    public ServiceLazy(Lazy<IServiceA> serviceA,
        Lazy<IServiceB> serviceB)
    {
        this._serviceA = serviceA;
        this._serviceB = serviceB;
    }

    public string Get()
    {
        var random = new Random().Next(1, 10);
        if (random % 2 == 0)
        {
            return this._serviceB.Value.Get();
        }

        return this._serviceA.Value.Get();
    }
}