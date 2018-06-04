using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Push;
using System.Collections.Generic;
using System.Linq;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Pushes produced packages in CodeCakeBuilder/Releases for projects that appear in
        /// <see cref="CheckRepositoryInfo.ActualPackagesToPublish"/> into <see cref="CheckRepositoryInfo.LocalFeedPath"/>
        /// and <see cref="CheckRepositoryInfo.RemoteFeed"/> if there are packages to push for each of them.
        /// </summary>
        /// <param name="globalInfo">The configured <see cref="CheckRepositoryInfo"/>.</param>
        /// <param name="releasesDir">The releasesDir (normally 'CodeCakeBuilder/Releases').</param>
        void StandardPushNuGetPackages( CheckRepositoryInfo globalInfo, string releasesDir )
        {
            // For packages: each of them must exist.
            IEnumerable<string> ToPackageFiles( IEnumerable<SolutionProject> projects )
            {
                return projects.Select( p => System.IO.Path.Combine( releasesDir, $"{p.Name}.{globalInfo.Version}.nupkg" ) );
            }
            // For symbols, handle the fact that they may not exist.
            IEnumerable<string> ToSymbolFiles( IEnumerable<SolutionProject> projects )
            {
                return projects
                        .Select( p => System.IO.Path.Combine( releasesDir, $"{p.Name}.{globalInfo.Version}.symbols.nupkg" ) )
                        .Select( p => new { Path = p, Exists = System.IO.File.Exists( p ) } )
                        .Where( p => p.Exists )
                        .Select( p => p.Path );
            }

            if( globalInfo.LocalFeedPath != null && globalInfo.LocalFeedPackagesToCopy.Count > 0 )
            {
                Cake.CopyFiles( ToPackageFiles( globalInfo.LocalFeedPackagesToCopy ), globalInfo.LocalFeedPath );
                Cake.CopyFiles( ToSymbolFiles( globalInfo.LocalFeedPackagesToCopy ), globalInfo.LocalFeedPath );
            }
            if( globalInfo.RemoteFeed != null && globalInfo.RemoteFeed.PackagesToPush.Count > 0 )
            {
                var settings = new NuGetPushSettings
                {
                    Source = globalInfo.RemoteFeed.PushUrl,
                    ApiKey = globalInfo.RemoteFeed.ActualAPIKey,
                    Verbosity = NuGetVerbosity.Detailed
                };
                foreach( var file in ToPackageFiles( globalInfo.RemoteFeed.PackagesToPush ) )
                {
                    Cake.Information( $"Pushing '{file}' to '{globalInfo.RemoteFeed.PushUrl}'." );
                    Cake.NuGetPush( file, settings );
                }
                if( globalInfo.RemoteFeed.PushSymbolUrl != null )
                {
                    NuGetPushSettings symbSettings = new NuGetPushSettings
                    {
                        Source = globalInfo.RemoteFeed.PushSymbolUrl,
                        ApiKey = globalInfo.RemoteFeed.ActualAPIKey,
                        Verbosity = NuGetVerbosity.Detailed
                    };
                    foreach( var file in ToSymbolFiles( globalInfo.RemoteFeed.PackagesToPush ) )
                    {
                        Cake.Information( $"Pushing Symbols '{file}' to '{globalInfo.RemoteFeed.PushSymbolUrl}'." );
                        Cake.NuGetPush( file, symbSettings );
                    }
                }
            }
        }

    }
}
