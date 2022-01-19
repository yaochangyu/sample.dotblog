// using System;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace AspNetCore3
// {
//     public static class ServiceCollectionEx
//     {
//         /// <summary>
//         /// Inject AddSingleton
//         /// </summary>
//         /// <typeparam name="TConfig"></typeparam>
//         /// <param name="services"></param>
//         /// <param name="configuration"></param>
//         /// <returns></returns>
//         public static TConfig Configure<TConfig>(this IServiceCollection services, IConfiguration configuration)
//             where TConfig : class, new()
//         {
//             if (services == null)
//             {
//                 throw new ArgumentNullException(nameof(services));
//             }
//
//             if (configuration == null)
//             {
//                 throw new ArgumentNullException(nameof(configuration));
//             }
//
//             var config = Activator.CreateInstance<TConfig>();
//             configuration.Bind(config);
//             services.AddSingleton(config);
//             return config;
//         }
//     }
// }

