using NuGet.Commands;
using NuGet.Commands.Test;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace sdkbootstrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            using var cacheContext = new SourceCacheContext();
                var restoreContext = new RestoreArgs()
            {
                CacheContext = cacheContext,
                DisableParallel = true,
                GlobalPackagesFolder = Path.Combine("c:\\work", "globalPackages"),
                Sources = new List<string>() { "C:\\work\\temp-cswinrt\\feed" } ,
                Log = new NullLogger(),
                CachingSourceProvider = new CachingSourceProvider(new TestPackageSourceProvider(new List<PackageSource>() { new PackageSource(pathContext.PackageSource) })),
                PreLoadedRequestProviders = new List<IPreLoadedRestoreRequestProvider>()
                    {
                        new DependencyGraphSpecRequestProvider(providerCache, dgFile)
                    }
            };

            var request = (await RestoreRunner.GetRequests(restoreContext)).Single();
            var providers = providerCache.GetOrCreate(pathContext.UserPackagesFolder, sources, new List<SourceRepository>(), cacheContext, logger);
            var command = new RestoreCommand(request.Request);

            // Add to cache before install on all providers
            var globalPackages = providers.GlobalPackages;
            var packages = globalPackages.FindPackagesById("a");
            packages.Should().BeEmpty("has not been installed yet");

            foreach (var local in providers.LocalProviders)
            {
                await local.GetDependenciesAsync(new LibraryIdentity("a", NuGetVersion.Parse("1.0.0"), LibraryType.Package), NuGetFramework.Parse("net46"), cacheContext, logger, CancellationToken.None);
            }

            // Install the package without updating the cache
            await SimpleTestPackageUtility.CreateFolderFeedV3Async(pathContext.UserPackagesFolder, PackageSaveMode.Defaultv3, packageA);

            // Run restore using an incorrect cache
            var result = await command.ExecuteAsync();
        }
    }
}
