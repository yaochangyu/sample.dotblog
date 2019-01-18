using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace Server
{
    public class SwaggerHelper
    {
        public static Func<IEnumerable<ApiDescription>, ApiDescription> ConflictingActionsResolver()
        {
            return apiDescriptions =>
                   {
                       var descriptions = apiDescriptions as ApiDescription[] ?? apiDescriptions.ToArray();

                       // 找第一個方法來改
                       var result = descriptions.First();

                       // 把所有的參數倒出來
                       var parameters = descriptions.SelectMany(d => d.ParameterDescriptions)
                                                    .ToList();

                       // 重整參數
                       result.ParameterDescriptions.Clear();
                       
                       foreach (var parameter in parameters)
                       {
                           if (result.ParameterDescriptions.All(x => x.Name != parameter.Name))
                           {
                               result.ParameterDescriptions.Add(new ApiParameterDescription
                               {
                                   Documentation = parameter.Documentation,
                                   Name = parameter.Name,
                                   ParameterDescriptor =
                                       new OptionalHttpParameterDescriptor((ReflectedHttpParameterDescriptor) parameter
                                                                               .ParameterDescriptor),
                                   Source = parameter.Source
                               });
                           }
                       }

                       return result;
                   };
        }
    }
}