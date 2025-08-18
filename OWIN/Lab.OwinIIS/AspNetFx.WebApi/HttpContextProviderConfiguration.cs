namespace AspNetFx.WebApi
{
    public enum HttpContextProviderType
    {
        AspNet,
        Owin
    }

    public static class HttpContextProviderConfiguration
    {
        private static HttpContextProviderType _providerType = HttpContextProviderType.AspNet;

        public static void UseAspNetProvider()
        {
            _providerType = HttpContextProviderType.AspNet;
        }

        public static void UseOwinProvider()
        {
            _providerType = HttpContextProviderType.Owin;
        }

        internal static HttpContextProviderType GetProviderType()
        {
            return _providerType;
        }
    }
}