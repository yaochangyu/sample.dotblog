namespace Sample.BO.V3
{
    public interface ICalculation
    {
        int Execute(int first, int second);

        int Execute(int first, int second, int third);
    }
}