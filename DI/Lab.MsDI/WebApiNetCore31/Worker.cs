namespace WebApiNetCore31
{
    public class Worker
    {
        public IMessager Messager { get; set; }

        public Worker(IMessager messager)
        {
            this.Messager = messager;
        }
    }

    public class Worker2
    {
        public IMessager Messager { get; set; }

        public Worker2(IMessager messager)
        {
            this.Messager = messager;
        }
    }
}