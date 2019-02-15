using System.Collections.Generic;
using System.Web.Http.Description;
using Swagger.Net;

namespace Server
{
    public class FileOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (operation.operationId.ToLower() == "file_upload")
            {
                if (operation.parameters == null)
                {
                    operation.parameters = new List<Parameter>();
                }
                else
                {
                    operation.parameters.Clear();
                }

                operation.parameters.Add(new Parameter
                {
                    name = "File1",
                    @in = "formData",
                    description = "Upload software package",
                    required = false,
                    type = "file"
                });
                operation.parameters.Add(new Parameter
                {
                    name = "File2",
                    @in = "formData",
                    description = "Upload software package",
                    required = false,
                    type = "file"
                });
                operation.description = "swagger upload";

                operation.consumes.Add("application/form-data");
            }
        }
    }
}