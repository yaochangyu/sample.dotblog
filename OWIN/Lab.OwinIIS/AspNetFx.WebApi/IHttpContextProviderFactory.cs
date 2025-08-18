namespace AspNetFx.WebApi
{
    public interface IHttpContextProviderFactory
    {
        IHttpContextProvider CreateProvider();
    }
}