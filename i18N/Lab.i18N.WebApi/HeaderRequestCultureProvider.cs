using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;

namespace Lab.i18N.WebApi;

public class HeaderRequestCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        // 檢查是否有 Accept-Language Header
        if (!httpContext.Request.Headers.TryGetValue("Accept-Language", out var acceptLanguage))
        {
            return await Task.FromResult<ProviderCultureResult?>(null);
        }

        var languages = acceptLanguage.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(lang => lang.Split(';').FirstOrDefault())
                .ToList()
            ;

        if (languages.Any())
        {
            var languageSegments = languages.Select(lang => new StringSegment(lang)).ToList();
            return await Task.FromResult(new ProviderCultureResult(languageSegments));
        }

        return await Task.FromResult<ProviderCultureResult?>(null);
    }
}