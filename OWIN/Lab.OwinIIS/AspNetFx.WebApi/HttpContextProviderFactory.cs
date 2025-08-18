namespace AspNetFx.WebApi
{
    public class HttpContextProviderFactory : IHttpContextProviderFactory
    {
        public IHttpContextProvider CreateProvider()
        {
            var providerType = HttpContextProviderConfiguration.GetProviderType();
            
            switch (providerType)
            {
                case HttpContextProviderType.Owin:
                    return new OWinHttpContextProvider();
                case HttpContextProviderType.AspNet:
                default:
                    return new HttpContextProvider();
            }
        }
    }
}