using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Simple.NugetDownloader;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task GetAllDependencyAsync()
        {
            var packageId = "cake.nuget";
            var version = "0.30.0";
            var availablePackages = await NugetManager.GetAllDependenciesAsync(packageId, version);
            foreach (var availablePackage in availablePackages)
            {
                Console.WriteLine($"Id:{availablePackage.Id},Version:{availablePackage.Version.Version}");
            }
        }

        [TestMethod]
        public async Task DownloadAsync()
        {
            var packageId = "cake.nuget";
            var version = "0.30.0";
            await NugetManager.DownloadAsync(packageId, version);
        }


        [TestMethod]
        public async Task GetAllPackagesAsync()
        {
            var packages = await NugetManager.GetAllPackagesAsync();
            var packageIdentities = new Dictionary<string, ICollection<PackageIdentity>>();

            foreach (var package in packages)
            {
                if (!packageIdentities.ContainsKey(package.Id))
                {
                    var identities = new List<PackageIdentity>();
                    identities.Add(package);
                    packageIdentities.Add(package.Id, identities);
                }
                else
                {
                    packageIdentities[package.Id].Add(package);
                }
            }
        }
    }
}