using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ErrorHandler.API.Controllers;

public class GenericController : ControllerBase
{
    public static readonly Dictionary<FailureCode, int> FailureCodeLookup = s_failureCodeLookupLazy.Value;
    private static readonly Lazy<Dictionary<FailureCode, int>> s_failureCodeLookupLazy = new(CreateFailureCodeLookup);

    private static Dictionary<string, int> CreateFailureCodeMappings()
    {
        //用關鍵字定義錯誤代碼
        return new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "error", StatusCodes.Status500InternalServerError },
            { "invalid", StatusCodes.Status400BadRequest },
            { "notfound", StatusCodes.Status404NotFound },
            { "concurrency", StatusCodes.Status429TooManyRequests },
            { "conflict", StatusCodes.Status404NotFound },
        };
    }

    [NonAction]
    public FailureObjectResult FailureContent(Failure failure)
    {
        if (string.IsNullOrWhiteSpace(failure.TraceId))
        {
            failure.TraceId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
        }

        if (FailureCodeLookup.TryGetValue(failure.Code, out int statusCode))
        {
            return new FailureObjectResult(failure, statusCode);
        }

        return new FailureObjectResult(failure);
    }

    private static Dictionary<FailureCode, int> CreateFailureCodeLookup()
    {
        var result = new Dictionary<FailureCode, int>();
        var type = typeof(FailureCode);
        var names = Enum.GetNames(type);
        var failureMappings = CreateFailureCodeMappings();
        foreach (var name in names)
        {
            var failureCode = FailureCode.Parse<FailureCode>(name);
            var isDefined = false;
            foreach (var mapping in failureMappings)
            {
                var key = mapping.Key;
                var statusCode = mapping.Value;
                if (name.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    isDefined = true;
                    result.Add(failureCode, statusCode);
                    break;
                }
            }

            if (isDefined == false)
            {
                result.Add(failureCode, StatusCodes.Status500InternalServerError);
            }
        }

        return result;
    }
}