using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Core;
using CodeCake.Abstractions;
using CSemVer;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Exposes global state information for the build script.
        /// </summary>
        public class CheckRepositoryInfo
        {
            public bool ObseleteConstructor;
            [Obsolete( "Use new CheckRepositoryInfo( ICakeContext ctx, SimpleRepositoryInfo gitInfo )" )]
            public CheckRepositoryInfo(SimpleRepositoryInfo gitInfo, IEnumerable<SolutionProject> projectsToPublish)
            {
                GitInfo = gitInfo;
                Version = SVersion.TryParse( gitInfo.SafeNuGetVersion );
                ObseleteConstructor = true;
            }
            public CheckRepositoryInfo( ICakeContext ctx, SimpleRepositoryInfo gitInfo )
            {
                GitInfo = gitInfo;
                Version = SVersion.TryParse( gitInfo.SafeNuGetVersion );
                Cake = ctx;
            }

            public ICakeContext Cake { get; }

            readonly List<ArtifactRepository> _artifactRepositories = new List<ArtifactRepository>();

            /// <summary>
            /// Gets the list of <see cref="ArtifactRepositories"/>
            /// </summary>
            public IEnumerable<ArtifactRepository> ArtifactRepositories => _artifactRepositories;

            public void AddAndInitRepository( ArtifactRepository artifactRepository )
            {
                artifactRepository.Init();
                _artifactRepositories.Add( artifactRepository );
            }

            /// <summary>
            /// Gets the SimpleRepositoryInfo from SimpleGitVersion.
            /// </summary>
            public SimpleRepositoryInfo GitInfo { get; }

            /// <summary>
            /// Gets or sets if the build is a release
            /// Defaults to false.
            /// </summary>
            public bool IsRelease { get; set; }

            [Obsolete("Use IsRelease instead, and do not use this. This is here only to not break previous Build.cs")]
            public NuGetRepositoryInfo BuildConfiguration => (NuGetRepositoryInfo)ArtifactRepositories.FirstOrDefault(a => a is NuGetRepositoryInfo);

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

            /// Gets or sets the local feed path.
            /// Can be null if no local feed exists or if local feed should be ignored.
            /// </summary>
            public string LocalFeedPath { get; set; }


            bool _ignoreNoArtifactsToProduce;
            /// <summary>
            /// Gets or sets whether <see cref="NoPackagesToProduce"/> should be ignored.
            /// Defaults to false: by default if there is no packages to produce <see cref="ShouldStop"/> is true.
            /// </summary>
            public bool IgnoreNoArtifactsToProduce
            {
                get
                {
                    return Cake.Argument( "IgnoreNoPackagesToProduce", 'N' ) == 'Y' || _ignoreNoArtifactsToProduce;
                }
                set
                {
                    _ignoreNoArtifactsToProduce = value;
                }
            }

            public bool NoArtifactsToProduce => ArtifactRepositories.All( repo => repo.NoArtifactsToProduce );

            /// <summary>
            /// Gets whether there is any package to produce
            /// </summary>
            public bool ShouldStop => NoArtifactsToProduce && !IgnoreNoArtifactsToProduce;

            public void PushArtifacts( string releasesDir )
            {
                foreach( ArtifactRepository repo in ArtifactRepositories )
                {
                    repo.PushArtifacts( releasesDir );
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="CheckRepositoryInfo"/>. This selects the feeds (a local and/or remote one)
        /// When running on Appveyor, the build number is set.
        /// </summary>
        /// <param name="gitInfo">The git info.</param>
        /// <returns>A new info object.</returns>
        CheckRepositoryInfo StandardCheckRepositoryWithoutNuget( SimpleRepositoryInfo gitInfo )
        {
            var result = new CheckRepositoryInfo( Cake, gitInfo )
            {
                // We build in Debug for any prerelease except "rc": the last prerelease step is in "Release".
                IsRelease = gitInfo.IsValidRelease
                               && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
            };
            // By default:
            if( !gitInfo.IsValid )
            {
                if( Cake.InteractiveMode() != InteractiveMode.NoInteraction
                    && Cake.ReadInteractiveOption( "PublishDirtyRepo", "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                {
                    Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                    result.IgnoreNoArtifactsToProduce = true;
                }
                else
                {
                    // On Appveyor, we let the build run: this gracefully handles Pull Requests.
                    if( Cake.AppVeyor().IsRunningOnAppVeyor )
                    {
                        result.IgnoreNoArtifactsToProduce = true;
                    }
                    else
                    {
                        Cake.TerminateWithError( "Repository is not ready to be published." );
                    }
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
                        // Release or prerelease go to LocalFeed/Release
                        result.LocalFeedPath = System.IO.Path.Combine( localFeedRoot, "Release" );
                    }
                    System.IO.Directory.CreateDirectory( result.LocalFeedPath );
                }

                // Creating the right remote feed.
                if( !result.IsLocalCIRelease
                    && (Cake.InteractiveMode() == InteractiveMode.NoInteraction
                        || Cake.ReadInteractiveOption( "PushToRemote", "Push to Remote feeds?", 'Y', 'N' ) == 'Y') )
                {
                    result.PushToRemote = true;
                }
            }
            SetCIVersionOnRunner( result );
            return result;
        }

    }
}
