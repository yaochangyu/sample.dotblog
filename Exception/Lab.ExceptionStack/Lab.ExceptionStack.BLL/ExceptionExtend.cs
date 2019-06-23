using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lab.ExceptionStack.BLL
{
    public static class ExceptionExtend
    {
        public static IEnumerable<string> GetAllErrorDescription(this Exception source)
        {
            var stack   = new StackTrace(source, true);
            var frames  = stack.GetFrames();
            var builder = new StringBuilder();
            var results = new List<string>();
            foreach (var frame in frames)
            {
                var methodBase = frame.GetMethod();
                var attribute = methodBase.GetCustomAttributes(typeof(ErrorDescription), true)
                                          .Select(p => p as ErrorDescription)
                                          .FirstOrDefault();
                if (attribute == null)
                {
                    results.Add($"Method:{methodBase.Name}");
                    builder.AppendLine();
                }
                else
                {
                    results.Add($"{attribute.Description}");
                }
            }

            return results;
        }

        public static ErrorDescription GetCurrentErrorDescription(this Exception source)
        {
            var stack      = new StackTrace(source, true);
            var frame      = stack.GetFrame(0);
            var methodBase = frame.GetMethod();
            var attribute = methodBase.GetCustomAttributes(typeof(ErrorDescription), true)
                                      .Select(p => (ErrorDescription) p)
                                      .FirstOrDefault();
            return attribute;
        }
    }
}