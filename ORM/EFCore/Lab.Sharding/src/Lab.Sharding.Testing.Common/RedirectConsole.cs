namespace Lab.Sharding.Testing.Common;

public sealed class RedirectConsole : IDisposable
{
	private readonly Action<string> _logFunction;
	private readonly TextWriter _oldOut = Console.Out;
	private readonly StringWriter sw = new();

	public RedirectConsole(Action<string> logFunction)
	{
		this._logFunction = logFunction;
		Console.SetOut(this.sw);
	}

	public void Dispose()
	{
		Console.SetOut(this._oldOut);
		this.sw.Flush();
		this._logFunction(this.sw.ToString());
		this.sw.Dispose();
	}
}