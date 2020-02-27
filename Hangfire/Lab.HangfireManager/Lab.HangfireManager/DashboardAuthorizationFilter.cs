using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Lab.HangfireManager
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        //Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter 
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}