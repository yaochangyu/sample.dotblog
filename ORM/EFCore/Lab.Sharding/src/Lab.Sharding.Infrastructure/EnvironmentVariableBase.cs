namespace Lab.Sharding.Infrastructure;

public record EnvironmentVariableBase<T>
{
    public string KeyName { get; }

    public T Value { get; }

    public EnvironmentVariableBase(Func<string, T> converter, bool isRequired = true)
    {
        this.KeyName = this.GetType().Name;

        string rawValue;
        try
        {
            rawValue = Environment.GetEnvironmentVariable(this.KeyName);
        }
        catch
        {
            rawValue = null;
        }

        if (isRequired && string.IsNullOrWhiteSpace(rawValue))
        {
            throw new ArgumentNullException($"EnvironmentVariable({this.KeyName}) was required.");
        }

        this.Value = converter(rawValue);
    }
}

public record EnvironmentVariableBase : EnvironmentVariableBase<string>
{
    public EnvironmentVariableBase(bool isRequired = true) : base(x => x, isRequired)
    {
    }
}