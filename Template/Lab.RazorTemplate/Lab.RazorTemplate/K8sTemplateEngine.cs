using System.Text;

namespace Lab.RazorTemplate;

public class K8sTemplateEngine
{
    public async Task<string> RenderAsync(string templatePath,
        K8sValue k8sValue,
        Dictionary<string, object> k8sDynamicValue)
    {
        return await Razor.Templating.Core.RazorTemplateEngine.RenderAsync(templatePath,
            k8sValue,
            k8sDynamicValue);
    }
}
