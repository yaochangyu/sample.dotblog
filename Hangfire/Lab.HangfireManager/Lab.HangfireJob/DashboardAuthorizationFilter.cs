using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Lab.HangfireJob
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