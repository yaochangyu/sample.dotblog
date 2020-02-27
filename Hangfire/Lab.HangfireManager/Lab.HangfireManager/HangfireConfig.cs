using System;
using System.Linq;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Management;
using Owin;

namespace Lab.HangfireManager
{
    internal class HangfireConfig
    {
        private static Type[] GetModuleTypes()
        {
            //var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //var moduleDirectory = System.IO.Path.Combine(baseDirectory, "Modules");
            //var assembliePaths = System.IO.Directory.GetFiles(baseDirectory, "*.dll");
            //if (System.IO.Directory.Exists(moduleDirectory))
            //    assembliePaths = assembliePaths.Concat(System.IO.Directory.GetFiles(moduleDirectory, "*.dll")).ToArray();

            //var assemblies = assembliePaths.Select(f => System.Reflection.Assembly.LoadFile(f)).ToArray();
            var assemblies = new[] {typeof(AnalysisJob).Assembly};
            var moduleTypes = assemblies.SelectMany(f =>
                                                    {
                                                        try
                                                        {
                                                            return f.GetTypes();
                                                        }
                                                        catch (Exception)
                                                        {
                                                            return new Type[] { };
                                                        }
                                                    }
                                                   ) /*.Where(f => f.IsClass && typeof(IClientModule).IsAssignableFrom(f))*/
                                        .ToArray();

            return moduleTypes;
        }

        public static void Register(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                               .UseManagementPages(cc => cc.AddJobs(() => { return GetModuleTypes(); }))
                               .UseSqlServerStorage("Hangfire")
                               .UseConsole()
                ;

            app.UseHangfireDashboard("/hangfire");
            app.UseHangfireServer();
        }
    }
}