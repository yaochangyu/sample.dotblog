namespace TestProject1
{
    public interface IHello
    {
        string SayHello();
    }

    public class EnglishHello : IHello
    {
        public string SayHello()
        {
            return "Hello";
        }
    }

    public class FrenchHello : IHello
    {
        public string SayHello()
        {
            return "Bonjour";
        }
    }
}