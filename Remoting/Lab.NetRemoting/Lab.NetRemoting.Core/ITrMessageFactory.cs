namespace Lab.NetRemoting.Core
{
    public interface ITrMessageFactory
    {
        string Url { get; set; }

        ITrMessage CreateInstance();

        ITrMessage CreateInstance(string name);
    }
}