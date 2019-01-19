using System.Linq;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace Server
{
    public class RemoveNamespaceOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (operation.parameters == null)
            {
                return;
            }

            var parameter = operation.parameters.FirstOrDefault(p => p.name == "namespace");
            if (parameter != null)
            {
                operation.parameters.Remove(parameter);
            }
        }
    }
}