using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Simple.NugetV3
{
    public class NugetManager
    {
        public static async Task DownloadAsync(string packageId, string version)
        {
            var package = new PackageIdentity(packageId, NuGetVersion.Parse(version));

            var settings = Settings.LoadDefaultSettings(null);
            var globalFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

            var logger = NullLogger.Instance;
            var cancelToken = CancellationToken.None;

            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
            var downloadFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Download");
            using (var cacheContext = new SourceCacheContext())
            {
                var downloadContext = new PackageDownloadContext(cacheContext, downloadFolder, true);

                var repositories = sourceRepositoryProvider.GetRepositories();
                foreach (var repository in repositories)
                {
                    var downloadResult = await PackageDownloader.GetDownloadResourceResultAsync(repository,
                                                                                                package,
                                                                                                downloadContext,
                                                                                                globalFolder,
                                                                                                logger,
                                                                                                cancelToken);

                    if (downloadResult != null)
                    {
                        var downloadFile = $@"{downloadFolder}\{package.Id}.{package.Version}.nupkg";

                        using (var fileStream = File.Create(downloadFile))
                        {
                            downloadResult.PackageStream.Seek(0, SeekOrigin.Begin);
                            await downloadResult.PackageStream.CopyToAsync(fileStream);
                        }

                        break;
                    }
                }
            }
        }

        public static async Task<ISet<PackageIdentity>> GetAllDependenciesAsync(string packageId,
                                                                                string version)
        {
            var package = new PackageIdentity(packageId, NuGetVersion.Parse(version));
            var settings = Settings.LoadDefaultSettings(null);
            var nuGetFramework = NuGetFramework.AnyFramework;
            var logger = NullLogger.Instance;
            var cancelToken = CancellationToken.None;
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
            var availablePackages = new HashSet<PackageIdentity>(PackageIdentityComparer.Default);

            using (var cacheContext = new SourceCacheContext())
            {
                var repositories = sourceRepositoryProvider.GetRepositories();

                await GetDependenciesAsync(package,
                                           nuGetFramework,
                                           cacheContext,
                                           logger,
                                           repositories,
                                           availablePackages,
                                           cancelToken);
            }

            return availablePackages;
        }

        private static async Task GetDependenciesAsync(PackageIdentity package,
                                                       NuGetFramework framework,
                                                       SourceCacheContext cacheContext,
                                                       ILogger logger,
                                                       IEnumerable<SourceRepository> repositories,
                                                       ISet<PackageIdentity> availablePackages,
                                                       CancellationToken cancellation)
        {
            if (availablePackages.Contains(package))
            {
                return;
            }

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(package,
                                                                                 framework,
                                                                                 cacheContext,
                                                                                 logger,
                                                                                 cancellation);

                if (dependencyInfo != null)
                {
                    if (!availablePackages.Contains(dependencyInfo))
                    {
                        availablePackages.Add(dependencyInfo);
                        foreach (var dependency in dependencyInfo.Dependencies)
                        {
                            var dependPackage = new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion);
                            await GetDependenciesAsync(dependPackage,
                                                       framework,
                                                       cacheContext,
                                                       logger,
                                                       repositories,
                                                       availablePackages, cancellation);
                        }
                    }

                    break;
                }
            }
        }

        public static async Task<ISet<PackageIdentity>> GetAllPackagesAsync()
        {
            var logger = NullLogger.Instance;
            var resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            var cancelToken = CancellationToken.None;
            resourceProviders.AddRange(Repository.Provider.GetCoreV3());
            var settings = Settings.LoadDefaultSettings(null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
            var repositories = sourceRepositoryProvider.GetRepositories();
            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var sourceRepository = new SourceRepository(packageSource, resourceProviders);
            var availablePackages = new HashSet<PackageIdentity>(PackageIdentityComparer.Default);

            var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
            await GetAllPackages(0, 10, searchResource, repositories, logger, availablePackages, cancelToken);

            return availablePackages;
        }

        private static async Task GetAllPackages(int skip,
                                                 int take,
                                                 PackageSearchResource searchResource,
                                                 IEnumerable<SourceRepository> repositories,
                                                 ILogger logger,
                                                 ISet<PackageIdentity> availablePackages,
                                                 CancellationToken cancelToken)
        {
            var metadatas = await searchResource.SearchAsync(null,
                                                             new SearchFilter(false),
                                                             skip,
                                                             take,
                                                             logger,
                                                             cancelToken);

            var nuGetFramework = NuGetFramework.AnyFramework;

            foreach (var metadata in metadatas)
            {
                var id = metadata.Identity.Id;
                var versions = await metadata.GetVersionsAsync();
                foreach (var version in versions)
                {
                    var package = new PackageIdentity(id, new NuGetVersion(version.Version));
                    if (!availablePackages.Contains(package))
                    {
                        using (var cacheContext = new SourceCacheContext())
                        {
                            availablePackages.Add(package);

                            await GetDependenciesAsync(package,
                                                       nuGetFramework,
                                                       cacheContext,
                                                       logger,
                                                       repositories,
                                                       availablePackages,
                                                       cancelToken);
                        }
                    }
                }
            }

            var count = metadatas.Count();
            if (count == take)
            {
                skip = skip + take;
                await GetAllPackages(skip, take, searchResource, repositories, logger, availablePackages, cancelToken);
            }
        }
    }
}