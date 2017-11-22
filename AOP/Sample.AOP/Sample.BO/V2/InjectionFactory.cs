namespace Sample.BO.V2
{
    public class InjectionFactory
    {
        public static ICalculation Create()
        {
            ICalculation calculation = new Calculation();

            var decorate =
                (ICalculation) new ExceptionHandlerRealProxy<ICalculation>(calculation).GetTransparentProxy();

            decorate =
                (ICalculation) new AuthenticationRealProxy<ICalculation>(decorate).GetTransparentProxy();

            return decorate;
        }
    }
}