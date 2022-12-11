namespace TestProject1Lab.StepDependencyInjection.Test;

public class SysFileProvider
{
    private readonly string _name;

    public SysFileProvider(string name)
    {
        this._name = name;
    }

    public string GetPath()
    {
        return $"{this._name}:SysFileProvider";
    }
}