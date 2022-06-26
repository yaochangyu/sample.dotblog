using System.Net;
using System.Text;

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
        Console.WriteLine($"Render Result:\r\n{result}");
    }

    [TestMethod]
    public async Task 替換範本_1()
    {
        var templatePath = "EnvTemplate.cshtml";
        var k8sValue = new K8sValue();
        var k8sDynamicValues = new Dictionary<string, object>
        {
            ["Market"] = "TW",
            ["Environment"] = "Dev",
        };

        var engine = new K8sTemplateEngine();
        var result = await engine.RenderAsync(templatePath, k8sValue, k8sDynamicValues);
        Console.WriteLine();
        Console.WriteLine($"Render Result:\r\n{result}");
    }
}