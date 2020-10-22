using NuGet.Commands;
using NuGet.Commands.Test;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Test.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace sdkbootstrapper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var cacheContext = new SourceCacheContext();
            using var pathContext = new SimpleTestPathContext();
            var providerCache = new RestoreCommandProvidersCache();
            var dgFile = new DependencyGraphSpec();
            var sources = new List<string>() {pathContext.PackageSource};
            var restoreContext = new RestoreArgs()
            {
                CacheContext = cacheContext,
                DisableParallel = true,
                GlobalPackagesFolder = pathContext.UserPackagesFolder,
                Sources = sources,
                Log = new NullLogger(),
                CachingSourceProvider =
                    new CachingSourceProvider(new TestPackageSourceProvider(
                        new List<PackageSource>() {new PackageSource(pathContext.PackageSource)})),
                PreLoadedRequestProviders = new List<IPreLoadedRestoreRequestProvider>()
                {
                    new DependencyGraphSpecRequestProvider(providerCache, dgFile)
                }
            };

            var request = (await RestoreRunner.GetRequests(restoreContext)).Single();
            var providers = providerCache.GetOrCreate(pathContext.UserPackagesFolder, sources,
                new List<SourceRepository>(), cacheContext, new NullLogger());
            var command = new RestoreCommand(request.Request);

            // Add to cache before install on all providers
            var globalPackages = providers.GlobalPackages;
            var packages = globalPackages.FindPackagesById("a");

            foreach (var local in providers.LocalProviders)
            {
                await local.GetDependenciesAsync(
                    new LibraryIdentity("a", NuGetVersion.Parse("1.0.0"), LibraryType.Package),
                    NuGetFramework.Parse("net46"), cacheContext, new NullLogger(), CancellationToken.None);
            }

            // Run restore using an incorrect cache
            var result = await command.ExecuteAsync();
        }
    }
}
