using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lab.ErrorHandler.API.Filters;

public class ModelValidationAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext actionContext)
    {
        // if (actionContext.Result != null)
        // {
        //     return;
        // }
        //
        // if (actionContext.ModelState.IsValid)
        // {
        //     return;
        // }

        var traceId = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;

        //處理 JSON Path
        var jsonPathKeys = actionContext.ModelState.Keys.Where(e => e.StartsWith("$.")).ToList();
        if (jsonPathKeys.Count > 0)
        {
            var errorData = new Dictionary<string, string>();
            foreach (var key in jsonPathKeys)
            {
                var normalizedKey = key.Substring(2);
                foreach (var error in actionContext.ModelState[key].Errors)
                {
                    if (error.Exception != null)
                    {
                        actionContext.ModelState.TryAddModelException(normalizedKey, error.Exception);
                    }

                    actionContext.ModelState.TryAddModelError(normalizedKey, "The provided value is not valid.");
                    errorData.Add(normalizedKey, error.ErrorMessage);
                }

                actionContext.ModelState.Remove(key);
            }

            //複寫錯誤內容
            actionContext.Result = new BadRequestObjectResult(new Failure
            {
                Code = FailureCode.InputInvalid,
                Message = "enum invalid",
                Data = errorData,
                TraceId = traceId
            });
            return;
        }

        var errors = actionContext.ModelState
            .Where(p => p.Value.ValidationState == ModelValidationState.Invalid)
            .ToDictionary(
                p => p.Key,
                p => p.Value.Errors.Select(e => e.ErrorMessage).ToList());

        //複寫錯誤內容
        actionContext.Result = new BadRequestObjectResult(new Failure()
        {
            Code = FailureCode.InputInvalid,
            Message = "input invalid",
            Data = errors,
            TraceId = traceId
        });
    }
}