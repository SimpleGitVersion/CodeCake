using System;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Pushes produced packages in CodeCakeBuilder/Releases for projects that appear in
        /// <see cref="NuGetRepositoryInfo.ActualPackagesToPublish"/> into <see cref="NuGetRepositoryInfo.LocalFeedPath"/>
        /// and <see cref="NuGetRepositoryInfo.Feeds"/> if there are packages to push for each of them.
        /// </summary>
        /// <param name="globalInfo">The configured <see cref="NuGetRepositoryInfo"/>.</param>
        /// <param name="releasesDir">The releasesDir (normally 'CodeCakeBuilder/Releases').</param>
        [Obsolete]
        void StandardPushNuGetPackages( CheckRepositoryInfo globalInfo, string releasesDir )
        {
            StandardPushNuGetPackages( globalInfo.BuildConfiguration, releasesDir );//This is awful, but it work !
        }

        void StandardPushNuGetPackages( NuGetRepositoryInfo nugetInfo, string releasesDir )
        {
            nugetInfo.PushArtifacts(releasesDir);
        }

    }
}
