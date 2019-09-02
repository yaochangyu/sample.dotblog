using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;

namespace Server
{
    public static class AutofacConfig
    {
        public static void Register()
        {
            // === 1. 建立容器 ===
            var builder = new ContainerBuilder();

            // === 2. 註冊服務 ===
            //取得目前執行App的Assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            //如果要註冊的的物件不再同一個Assembly則用
            //Assembly assembly = typeof(OtherService).Assembly;

            /*下面有幾種常用註冊方法*/
            //A.直接註冊某個物件
            builder.RegisterType<MyService>().As<IService>(); //表示註冊MyService這個Class並且為IMyService這個物件的實作

            //B.註冊所有名稱為Service結尾的物件
            //builder.RegisterAssemblyTypes(assembly)
            //       .Where(x => x.Name.EndsWith("Service", StringComparison.Ordinal))
            //       .AsImplementedInterfaces();

            //C.註冊所有父類別為BaseMethod的物件
            //builder.RegisterAssemblyTypes(assembly)
            //       .Where(x => x.BaseType == typeof(BaseMethod)).AsImplementedInterfaces();

            ////D.註冊實作某個介面的物件
            //builder.RegisterAssemblyTypes(assembly)
            //       .Where(x => x.GetInterfaces().Contains(typeof(ICommonMethod))).AsImplementedInterfaces();

            //***重要*** 註冊Controller和ApiController
            builder.RegisterControllers(assembly);
            builder.RegisterApiControllers(assembly);

            // === 3. 由Builder建立容器 ===
            var container = builder.Build();


            // === 4. 把容器設定給DependencyResolver ===
            var resolverApi = new AutofacWebApiDependencyResolver(container);
            GlobalConfiguration.Configuration.DependencyResolver = resolverApi;
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}