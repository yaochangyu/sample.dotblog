using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lab.ExceptionStack.BLL
{
    public static class ExceptionExtend
    {
        public static ErrorDescriptionAttribute GetCurrentErrorDescription(this Exception source)
        {
            var stack      = new StackTrace(source, true);
            var frame      = stack.GetFrame(0);
            var methodBase = frame.GetMethod();
            var attribute = methodBase.GetCustomAttributes(typeof(ErrorDescriptionAttribute), true)
                                      .Select(p => (ErrorDescriptionAttribute) p)
                                      .FirstOrDefault();
            return attribute;
        }
    }
}