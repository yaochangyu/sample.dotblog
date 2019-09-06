using System.Linq;
using System.Threading;
using System.Web.Http.Description;
using Swagger.Net;

namespace Server2.Filters
{
    public class CancellationTokenOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var excludedParameters = apiDescription.ParameterDescriptions
                                                   .Where(p => p.ParameterDescriptor.ParameterType == typeof(CancellationToken))
                                                   .Select(p => operation.parameters.FirstOrDefault(operationParam => operationParam.name == p.Name))
                                                   .ToArray();

            foreach (var parameter in excludedParameters)
                operation.parameters.Remove(parameter);
        }
    }
}