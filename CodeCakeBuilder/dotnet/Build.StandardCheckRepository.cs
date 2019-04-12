using Cake.Common.Solution;
using SimpleGitVersion;
using System;
using System.Collections.Generic;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Creates a new <see cref="CheckRepositoryInfo"/> with a <see cref="NuGetRepositoryInfo"/>. This selects the feeds (a local and/or remote one)
        /// When running on Appveyor, the build number is set.
        /// </summary>
        /// <param name="gitInfo">The git info.</param>
        /// <returns>A new info object.</returns>
        [Obsolete( "Use StandardCheckRepositoryWithoutNuGet and add the NuGet repository yourself." )]
#pragma warning disable IDE0051 // Remove unused private members
        CheckRepositoryInfo StandardCheckRepository( IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo )
#pragma warning restore IDE0051 // Remove unused private members
        {
            CheckRepositoryInfo checkInfo = StandardCheckRepositoryWithoutNuget( gitInfo );
            checkInfo.AddAndInitRepository( new NuGetRepositoryInfo( Cake, checkInfo, projectsToPublish ) );
            return checkInfo;
        }

    }
}
