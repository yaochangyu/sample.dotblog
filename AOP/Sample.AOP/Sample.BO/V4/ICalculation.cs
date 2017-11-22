namespace Sample.BO.V4
{
    public interface ICalculation
    {
        int Execute(int first, int second);

        int Execute(int first, int second, int third);
    }
}