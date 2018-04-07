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
        /// and <see cref="CheckRepositoryInfo.RemoteFeed"/> if they are not null.
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
                if( Cake.InteractiveMode() != InteractiveMode.NoInteraction )
                {
                    if( Cake.ReadInteractiveOption( "LocalFeed", "Do you want to publish to LocalFeed?", 'N', 'Y' ) == 'Y' )
                    {
                        Cake.CopyFiles( ToPackageFiles( globalInfo.LocalFeedPackagesToCopy ), globalInfo.LocalFeedPath );
                        Cake.CopyFiles( ToSymbolFiles( globalInfo.LocalFeedPackagesToCopy ), globalInfo.LocalFeedPath );
                    }
                }
            }
            if( globalInfo.RemoteFeed != null && globalInfo.RemoteFeed.PackagesToPush.Count > 0 )
            {
                var apiKey = Cake.InteractiveEnvironmentVariable( globalInfo.RemoteFeed.APIKeyName );
                if( string.IsNullOrEmpty( apiKey ) )
                {
                    Cake.Information( $"Could not resolve {globalInfo.RemoteFeed.APIKeyName}. Push to {globalInfo.RemoteFeed.PushUrl} is skipped." );
                }
                else
                {
                    var settings = new NuGetPushSettings
                    {
                        Source = globalInfo.RemoteFeed.PushUrl,
                        ApiKey = apiKey,
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
                            ApiKey = apiKey,
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
}
