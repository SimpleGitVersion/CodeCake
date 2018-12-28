using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using CK.Text;
using CSemVer;
using SimpleGitVersion;
using System.Collections.Generic;
using System.Linq;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Exposes global state information for the build script.
        /// </summary>
        class CheckRepositoryInfo
        {
            /// <summary>
            /// Gets the remote target feeds.
            /// (This is extracted as an independent function to be more easily transformable.)
            /// </summary>
            /// <returns></returns>
            public static IEnumerable<NuGetHelper.Feed> GetTargetRemoteFeeds()
            {
                return new NuGetHelper.Feed[]{

new SignatureVSTSFeed( "Signature-OpenSource", "Default" )
};
            }

            public CheckRepositoryInfo( SimpleRepositoryInfo gitInfo, IEnumerable<SolutionProject> projectsToPublish )
            {
                GitInfo = gitInfo;
                Version = SVersion.TryParse( gitInfo.SafeNuGetVersion );
                if( Version.IsValid )
                {
                    NuGetPackagesToPublish = projectsToPublish.Select( p => new SimplePackageId( p.Name, Version ) ).ToList();
                }
            }

            /// <summary>
            /// Gets the SimpleRepositoryInfo from SimpleGitVersion.
            /// </summary>
            public SimpleRepositoryInfo GitInfo { get; }

            /// <summary>
            /// Gets or sets the build configuration: either "Debug" or "Release".
            /// Defaults to "Debug".
            /// </summary>
            public string BuildConfiguration { get; set; } = "Debug";

            /// <summary>
            /// Gets the version of the packages.
            /// <see cref="SVersion.IsValid"/> is false if the working folder is not ready to be published.
            /// </summary>
            public SVersion Version { get; }

            /// <summary>
            /// Gets the version of the packages, without any build meta information (if any).
            /// </summary>
            public string FilePartVersion => Version.NormalizedText;

            /// <summary>
            /// Gets whether this is a purely local build.
            /// </summary>
            public bool IsLocalCIRelease { get; set; }

            /// <summary>
            /// Gets whether remote feeds, stores or any other external repositories should receive atrifacts.
            /// </summary>
            public bool PushToRemote { get; set; }

            /// <summary>
            /// Gets all the NuGet packages to publish. This is a simple projection of
            /// the projectsToPublish script variable.
            /// </summary>
            public IReadOnlyList<SimplePackageId> NuGetPackagesToPublish { get; }

            /// Gets or sets the local feed path.
            /// Can be null if no local feed exists or if local feed should be ignored.
            /// </summary>
            public string LocalFeedPath { get; set; }

            /// <summary>
            /// Gets the mutable list of remote feeds to which packages should be pushed.
            /// </summary>
            public List<NuGetHelper.Feed> Feeds { get; } = new List<NuGetHelper.Feed>();

            /// <summary>
            /// Gets the union of <see cref="Feeds"/>'s <see cref="NuGetHelper.Feed.PackagesToPublish"/> without duplicates.
            /// </summary>
            public IEnumerable<SimplePackageId> ActualPackagesToPublish => Feeds.SelectMany( f => f.PackagesToPublish ).Distinct();

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
            public bool NoPackagesToProduce => !Feeds.SelectMany( f => f.PackagesToPublish ).Any();
        }

        /// <summary>
        /// Creates a new <see cref="CheckRepositoryInfo"/>. This selects the feeds (a local and/or remote one)
        /// and checks the packages that sould actually be produced for them.
        /// When running on Appveyor, the build number is set.
        /// </summary>
        /// <param name="projectsToPublish">The projects to publish.</param>
        /// <param name="gitInfo">The git info.</param>
        /// <returns>A new info object.</returns>
        CheckRepositoryInfo StandardCheckRepository( IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo )
        {
            var result = new CheckRepositoryInfo( gitInfo, projectsToPublish );

            // We build in Debug for any prerelease except "rc": the last prerelease step is in "Release".
            result.BuildConfiguration = gitInfo.IsValidRelease
                                        && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
                                        ? "Release"
                                        : "Debug";
            // By default:
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
                // If a /LocalFeed/ directory exists above, we publish the packages in it.
                var localFeedRoot = Cake.FindDirectoryAbove( "LocalFeed" );
                if( localFeedRoot != null )
                {
                    var v = gitInfo.Info.FinalSemVersion;
                    if( v.AsCSVersion == null )
                    {
                        if( v.Prerelease.EndsWith( ".local" ) )
                        {
                            // Local releases must not be pushed on any remote and are copied to LocalFeed/Local
                            // feed (if LocalFeed/ directory above exists).
                            result.IsLocalCIRelease = true;
                            result.LocalFeedPath = System.IO.Path.Combine( localFeedRoot, "Local" );
                        }
                        else
                        {
                            // CI build versions are routed to LocalFeed/CI
                            result.LocalFeedPath = System.IO.Path.Combine( localFeedRoot, "CI" );
                        }
                    }
                    else
                    {
                        // Relaease or prerelease go to LocalFeed/Release
                        result.LocalFeedPath = System.IO.Path.Combine( localFeedRoot, "Release" );
                    }
                    System.IO.Directory.CreateDirectory( result.LocalFeedPath );
                }

                if( result.LocalFeedPath != null )
                {
                    Cake.Information( $"Adding NuGet local feed: {result.LocalFeedPath}" );
                    result.Feeds.Add( new LocalFeed( result.LocalFeedPath ) );
                }

                // Creating the right remote feed.
                if( !result.IsLocalCIRelease
                    && Cake.ReadInteractiveOption( "PushToRemote", "Push to Remote feeds?", 'Y', 'N' ) == 'Y' )
                {
                    result.PushToRemote = true; 
                    foreach( var f in CheckRepositoryInfo.GetTargetRemoteFeeds() )
                    {
                        Cake.Information( $"Adding NuGet remote feed: {f.Name} => {f.Url}" );
                        result.Feeds.Add( f );
                    }
                }
            }

            // Now that Local/RemoteFeeds are selected, we can check the packages that already exist
            // in those feeds.
            var all = result.Feeds.Select( f => f.InitializePackagesToPublishAsync( Cake, result.NuGetPackagesToPublish ) );
            System.Threading.Tasks.Task.WaitAll( all.ToArray() );
            foreach( var feed in result.Feeds )
            {
                feed.Information( Cake, result.NuGetPackagesToPublish );
            }

            int nbPackagesToPublish = result.ActualPackagesToPublish.Count();
            if( nbPackagesToPublish == 0 )
            {
                Cake.Information( $"No packages out of {projectsToPublish.Count()} projects to publish." );
                if( Cake.Argument( "IgnoreNoPackagesToProduce", 'N' ) == 'Y' )
                {
                    result.IgnoreNoPackagesToProduce = true;
                }
            }
            else
            {
                Cake.Information( $"Should actually publish {nbPackagesToPublish} out of {projectsToPublish.Count()} projects with version={gitInfo.SafeNuGetVersion} and configuration={result.BuildConfiguration}: {result.ActualPackagesToPublish.Select( p => p.PackageId ).Concatenate()}" );
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
