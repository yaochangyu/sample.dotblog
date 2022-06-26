namespace Lab.RazorTemplate.Test;

[TestClass]
public class K8sTemplateEngineTests
{
    [TestMethod]
    public async Task 替換範本()
    {
        var templatePath = "Template.ConfigMap.cshtml";
        var k8sValue = new K8sValue()
        {
            Common = new Common
            {
                ProjectName = "member-service-api",
                Namespace = "member-service",
            },
        };
        var k8sDynamicValues = new Dictionary<string, object>
        {
            ["Value1"] = "1",
            ["Value2"] = "2",
            ["K8S_COMMON_SERVICE_NAME"] = "3",
        };
        var engine = new K8sTemplateEngine();
        var result = await engine.RenderAsync(templatePath, k8sValue, k8sDynamicValues);
        Console.WriteLine(result);
    }
}