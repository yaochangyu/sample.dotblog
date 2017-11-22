namespace Sample.BO.V1
{
    public class CalculationFactory
    {
        public static ICalculation Create()
        {
            ICalculation dynamicProxy = new CalculationProxy();
            return dynamicProxy;
        }
    }
}