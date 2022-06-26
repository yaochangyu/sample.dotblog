namespace Lab.RazorTemplate;

public class K8sValue
{
    public Common Common { get; set; }

    public Resource Resource { get; set; }
}

public class Common
{
    public string ProjectName { get; set; }

    public string Namespace { get; set; }
}

public class Resource
{
    public uint CPU { get; set; }

    public uint Memory { get; set; }
}
