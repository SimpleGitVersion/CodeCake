using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Core;
using CK.Text;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Base class that defines a NuGet feed.
        /// </summary>
        abstract class NuGetRemoteFeed
        {
            /// <summary>
            /// Gets the push API key name.
            /// This is the environment variable name. 
            /// </summary>
            public string APIKeyName { get; protected set; }

            /// <summary>
            /// Gets the push url.
            /// </summary>
            public string PushUrl { get; protected set; }

            /// <summary>
            /// Gets the push symbol url.
            /// Can be null: pushing the symbol is skipped.
            /// </summary>
            public string PushSymbolUrl { get; protected set; }

            /// <summary>
            /// Gets a mutable list of SolutionProject for which packages should be created and 
            /// pushed to this feed.
            /// </summary>
            public List<SolutionProject> PackagesToPush { get; } = new List<SolutionProject>();

            /// <summary>
            /// Checks whether a given package exists in this feed.
            /// </summary>
            /// <param name="ctx">The cake context (mainly used to log).</param>
            /// <param name="client">The <see cref="HttpClient"/> to use.</param>
            /// <param name="packageId">The package name.</param>
            /// <param name="version">The package version.</param>
            /// <returns>True if the package exists, false otherwise.</returns>
            public abstract Task<bool> CheckPackageAsync( ICakeContext ctx, HttpClient client, string packageId, string version );

            /// <summary>
            /// Gets or sets the actual api key that should be obtained from <see cref="APIKeyName"/>.
            /// </summary>
            public string ActualAPIKey { get; set; }
        }

        class MyGetPublicFeed : NuGetRemoteFeed
        {
            readonly string _feedName;

            public MyGetPublicFeed( string feedName, string apiKeyName )
            {
                _feedName = feedName;
                APIKeyName = apiKeyName;
                PushUrl = $"https://www.myget.org/F/{feedName}/api/v2/package";
                PushSymbolUrl = $"https://www.myget.org/F/{feedName}/symbols/api/v2/package";
            }

            public override async Task<bool> CheckPackageAsync( ICakeContext ctx, HttpClient client, string packageId, string version )
            {
                // My first idea was to challenge the Manual Download url with a Head, unfortunately myget
                // returns a 501 not implemented. I use the html page for the package.
                // Caution: The HttpClient must not follow the redirect here!
                try
                {
                    var page = $"https://www.myget.org/feed/{_feedName}/package/nuget/{packageId}/{version}";
                    using( var m = new HttpRequestMessage( HttpMethod.Head, new Uri( page ) ) )
                    using( var r = await client.SendAsync( m ) )
                    {
                        return r.StatusCode == System.Net.HttpStatusCode.OK;
                    }
                }
                catch( Exception ex )
                {
                    ctx.Warning( $"Unable to check that package {packageId} exists on MyGet: {ex.Message}" );
                    ctx.Warning( $"Considering that it does not exist." );
                    return false;
                }
            }
        }


        /// <summary>
        /// Exposes global state information for the build script.
        /// </summary>
        class CheckRepositoryInfo
        {
            /// <summary>
            /// Gets or sets the build configuration: either "Debug" or "Release".
            /// Defaults to "Debug".
            /// </summary>
            public string BuildConfiguration { get; set; } = "Debug";

            /// <summary>
            /// Gets or sets the version of the packages.
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// Gets or sets the local feed path to which <see cref="LocalFeedPackagesToCopy"/> should be copied.
            /// Can be null if no local feed exists or if no push to local feed should be done.
            /// </summary>
            public string LocalFeedPath { get; set; }

            /// <summary>
            /// Gets whether this is a blank build.
            /// </summary>
            public bool IsBlankCIRelease { get; set; }

            /// <summary>
            /// Gets a mutable list of SolutionProject for which packages should be created and copied
            /// to the <see cref="LocalFeedPath"/>.
            /// </summary>
            public List<SolutionProject> LocalFeedPackagesToCopy { get; } = new List<SolutionProject>();

            /// <summary>
            /// Gets or sets the remote feed to which packages should be pushed.
            /// </summary>
            public NuGetRemoteFeed RemoteFeed { get; set; }

            /// <summary>
            /// Gets the union of <see cref="LocalFeedPackagesToCopy"/> and <see cref="RemoteFeed"/>'s
            /// <see cref="NuGetRemoteFeed.PackagesToPush"/> without duplicates.
            /// </summary>
            public IEnumerable<SolutionProject> ActualPackagesToPublish => LocalFeedPackagesToCopy.Concat( RemoteFeed?.PackagesToPush ?? Enumerable.Empty<SolutionProject>() ).Distinct();

            /// <summary>
            /// Gets whether it is useless to continue. By default if <see cref="NoPackagesToProduce"/> is true, this is true,
            /// but if <see cref="IgnoreNoPackagesToProduce"/> is set, then we should continue.
            /// </summary>
            public bool ShouldStop => NoPackagesToProduce && !IgnoreNoPackagesToProduce;

            /// <summary>
            /// Gets or sets whether <see cref="NoPackagesToProduce"/> should be ignored.
            /// Defaults to false: by default if there is no packages to produce <see cref="ShouldStop"/> is true.
            /// </summary>
            public bool IgnoreNoPackagesToProduce { get; set; }

            /// <summary>
            /// Gets whether there is at least one package to produce and push.
            /// </summary>
            public bool NoPackagesToProduce => (LocalFeedPath == null || LocalFeedPackagesToCopy.Count == 0)
                                               &&
                                               (RemoteFeed == null || RemoteFeed.PackagesToPush.Count == 0);
        }

        /// <summary>
        /// Creates a new <see cref="CheckRepositoryInfo"/>. This selects the feeds (a local and/or e remote one)
        /// and checks the packages that sould actually be produced for them.
        /// When running on Appveyor, the build number is set.
        /// </summary>
        /// <param name="projectsToPublish">The projects to publish.</param>
        /// <param name="gitInfo">The git info.</param>
        /// <returns>A new info object.</returns>
        CheckRepositoryInfo StandardCheckRepository( IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo )
        {
            // Local function that displays information for packages already in a feed or not.
            void DispalyFeedPackageResult( string feedId, IReadOnlyList<SolutionProject> missingPackages, int totalCount )
            {
                var missingCount = missingPackages.Count;
                var existCount = totalCount - missingCount;

                if( missingCount == 0 )
                {
                    Cake.Information( $"{feedId}: No packages must be pushed ({existCount} packages already available)." );
                }
                else if( existCount == 0 )
                {
                    Cake.Information( $"{feedId}: All {missingCount} packages must be pushed." );
                }
                else
                {
                    Cake.Information( $"{feedId}: {missingCount} packages must be pushed: {missingPackages.Select( p => p.Name ).Concatenate()}." );
                    Cake.Information( $"    => {existCount} packages already pushed: {projectsToPublish.Except( missingPackages ).Select( p => p.Name ).Concatenate()}." );
                }
            }

            var result = new CheckRepositoryInfo { Version = gitInfo.SafeNuGetVersion };

            // We build in Debug for any prerelease except "rc": the last prerelease step is in "Release".
            result.BuildConfiguration = gitInfo.IsValidRelease
                                        && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
                                        ? "Release"
                                        : "Debug";

            if( !gitInfo.IsValid )
            {
                if( Cake.InteractiveMode() != InteractiveMode.NoInteraction
                    && Cake.ReadInteractiveOption( "PublishDirtyRepo", "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                {
                    Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                    result.IgnoreNoPackagesToProduce = true;
                }
                else
                {
                    // On Appveyor, we let the build run: this gracefully handles Pull Requests.
                    if( Cake.AppVeyor().IsRunningOnAppVeyor )
                    {
                        result.IgnoreNoPackagesToProduce = true;
                    }
                    else Cake.TerminateWithError( "Repository is not ready to be published." );
                }
                // When the gitInfo is not valid, we do not try to push any packages, even if the build continues
                // (either because the user choose to continue or if we are on the CI server).
                // We don't need to worry about feeds here.
            }
            else
            {
                // gitInfo is valid: it is either ci or a release build. 
                // Blank releases must not be pushed on any remote and are compied to LocalFeed/Blank
                // local feed it it exists.
                bool isBlankCIRelease = gitInfo.Info.FinalSemVersion.Prerelease.EndsWith( ".blank" );
                var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                if( localFeed != null && isBlankCIRelease )
                {
                    localFeed = System.IO.Path.Combine( localFeed, "Blank" );
                    System.IO.Directory.CreateDirectory( localFeed );
                }
                result.IsBlankCIRelease = isBlankCIRelease;
                result.LocalFeedPath = localFeed;

                // Creating the right NuGetRemoteFeed according to the release level.
                if( !isBlankCIRelease )
                {
                    if( gitInfo.IsValidRelease )
                    {
                        if( gitInfo.PreReleaseName == ""
                            || gitInfo.PreReleaseName == "prerelease"
                            || gitInfo.PreReleaseName == "rc" )
                        {
                            result.RemoteFeed = new MyGetPublicFeed( "invenietis-release", "MYGET_RELEASE_API_KEY" );
                        }
                        else
                        {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                            result.RemoteFeed = new MyGetPublicFeed( "invenietis-preview", "MYGET_PREVIEW_API_KEY" );
                        }
                    }
                    else
                    {
                        Debug.Assert( gitInfo.IsValidCIBuild );
                        result.RemoteFeed = new MyGetPublicFeed( "invenietis-ci", "MYGET_CI_API_KEY" );
                    }
                }
            }

            // Now that Local/RemoteFeed are selected, we can check the packages that already exist
            // in those feeds.
            if( result.RemoteFeed != null )
            {
                using( var client = new HttpClient( new HttpClientHandler { AllowAutoRedirect = false }, true ) )
                {
                    var requests = projectsToPublish
                                    .Select( p => new
                                    {
                                        Project = p,
                                        ExistsAsync = result.RemoteFeed.CheckPackageAsync( Cake, client, p.Name, gitInfo.SafeNuGetVersion )
                                    } )
                                    .ToList();
                    System.Threading.Tasks.Task.WaitAll( requests.Select( r => r.ExistsAsync ).ToArray() );
                    var notOk = requests.Where( r => !r.ExistsAsync.Result ).Select( r => r.Project );
                    result.RemoteFeed.PackagesToPush.AddRange( notOk );
                    DispalyFeedPackageResult( result.RemoteFeed.PushUrl, result.RemoteFeed.PackagesToPush, requests.Count );
                    // If there is at least a package to push, challenge the key right now: if the key can not be obtained, then
                    // we clear the list.
                    var apiKey = Cake.InteractiveEnvironmentVariable( result.RemoteFeed.APIKeyName );
                    if( string.IsNullOrEmpty( apiKey ) )
                    {
                        Cake.Information( $"Could not resolve {result.RemoteFeed.APIKeyName}. Push to {result.RemoteFeed.PushUrl} is skipped." );
                        result.RemoteFeed.PackagesToPush.Clear();
                    }
                    else result.RemoteFeed.ActualAPIKey = apiKey;
                }
            }
            if( result.LocalFeedPath != null )
            {
                var lookup = projectsToPublish
                                .Select( p => new
                                {
                                    Project = p,
                                    Path = System.IO.Path.Combine( result.LocalFeedPath, $"{p.Name}.{gitInfo.SafeNuGetVersion}.nupkg" )
                                } )
                                .Select( x => new
                                {
                                    x.Project,
                                    Exists = System.IO.File.Exists( x.Path )
                                } )
                                .ToList();
                var notOk = lookup.Where( r => !r.Exists ).Select( r => r.Project );
                result.LocalFeedPackagesToCopy.AddRange( notOk );
                DispalyFeedPackageResult( result.LocalFeedPath, result.LocalFeedPackagesToCopy, lookup.Count );
            }
            int nbPackagesToPublish = result.ActualPackagesToPublish.Count();
            if( nbPackagesToPublish == 0 )
            {
                Cake.Information( $"No packages out of {projectsToPublish.Count()} projects to publish." );
            }
            else
            {
                Cake.Information( $"Should actually publish {nbPackagesToPublish} out of {projectsToPublish.Count()} projects with version={gitInfo.SafeNuGetVersion} and configuration={result.BuildConfiguration}: {result.ActualPackagesToPublish.Select( p => p.Name ).Concatenate()}" );
            }
            var appVeyor = Cake.AppVeyor();
            if( appVeyor.IsRunningOnAppVeyor )
            {
                if( result.ShouldStop )
                {
                    appVeyor.UpdateBuildVersion( $"{gitInfo.SafeNuGetVersion} - Skipped ({appVeyor.Environment.Build.Number})" );
                }
                else
                {
                    try
                    {
                        appVeyor.UpdateBuildVersion( gitInfo.SafeNuGetVersion );
                    }
                    catch
                    {
                        appVeyor.UpdateBuildVersion( $"{gitInfo.SafeNuGetVersion} ({appVeyor.Environment.Build.Number})" );
                    }
                }
            }
            return result;
        }

    }
}
