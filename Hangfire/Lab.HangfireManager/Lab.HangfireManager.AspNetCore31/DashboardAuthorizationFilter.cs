using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Lab.HangfireManager.AspNetCore31
{
    public class DashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
    { //Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter 
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}