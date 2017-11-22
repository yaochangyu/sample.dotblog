namespace Sample.BO.V2
{
    public class CalculationFactory
    {
        public static ICalculation Create()
        {
            ICalculation calculation = new Calculation();

            var decorate =
                (ICalculation) new ExceptionHandlerProxy<ICalculation>(calculation).GetTransparentProxy();

            //decorate =
            //    (ICalculation) new AuthenticationProxy<ICalculation>(decorate).GetTransparentProxy();

            return decorate;
        }
    }
}