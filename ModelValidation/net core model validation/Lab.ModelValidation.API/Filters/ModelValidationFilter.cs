using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lab.ModelValidation.API.Filters;

public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Result != null)
        {
            return;
        }

        if (context.ModelState.IsValid)
        {
            return;
        }

        // 获取败的验证信息列表
        var errors = context.ModelState
            .Where(s => s.Value != null
                        && s.Value.ValidationState == ModelValidationState.Invalid)
            .SelectMany(s => s.Value!.Errors.ToList())
            .Select(e => e.ErrorMessage)
            .ToArray();

        // 回傳自訂檢查錯誤
        var result = new Failure()
        {
            Code = FailureCode.InputValid,
            Message = "input valid",
            Data = errors
        };

        context.Result = new BadRequestObjectResult(result);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}