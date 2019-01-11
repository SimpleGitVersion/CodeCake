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
        /// and <see cref="CheckRepositoryInfo.Feeds"/> if there are packages to push for each of them.
        /// </summary>
        /// <param name="globalInfo">The configured <see cref="CheckRepositoryInfo"/>.</param>
        /// <param name="releasesDir">The releasesDir (normally 'CodeCakeBuilder/Releases').</param>
        void StandardPushNuGetPackages( CheckRepositoryInfo globalInfo, string releasesDir )
        {
            var all = globalInfo.Feeds.Select( feed => feed.PushPackagesAsync( Cake, releasesDir, feed.PackagesToPublish ) );
            System.Threading.Tasks.Task.WaitAll( all.ToArray() );
        }

    }
}
