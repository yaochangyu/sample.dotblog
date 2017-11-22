namespace Sample.BO.V1
{
    public class InjectionFactory
    {
        public static ICalculation Create()
        {
            ICalculation dynamicProxy = new CalculationProxy();
            return dynamicProxy;
        }
    }
}