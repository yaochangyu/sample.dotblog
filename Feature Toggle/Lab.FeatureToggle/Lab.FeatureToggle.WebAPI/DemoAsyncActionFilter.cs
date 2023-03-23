using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.FeatureToggle.WebAPI;

public class DemoAsyncActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        Console.WriteLine("on action execution");

        // Do something before the action executes.
        await next();

        // Do something after the action executes.
    }
}