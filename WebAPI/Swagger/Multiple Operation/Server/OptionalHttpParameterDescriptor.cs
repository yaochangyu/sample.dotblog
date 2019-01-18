using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;

namespace Server
{
    // this inheritance is required, as IsOptional has only getter
    public class OptionalHttpParameterDescriptor : ReflectedHttpParameterDescriptor
    {
        public OptionalHttpParameterDescriptor(ReflectedHttpParameterDescriptor parameterDescriptor)
            : base(parameterDescriptor.ActionDescriptor, parameterDescriptor.ParameterInfo)
        {
        }
        public override bool IsOptional => true;
    }
}