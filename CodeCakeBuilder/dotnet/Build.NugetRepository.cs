using Cake.Common.Solution;
using Cake.Core;
using CodeCake.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Exposes global state information for the build script.
        /// </summary>
        public class NuGetRepositoryInfo : ArtifactRepository
        {
            /// <summary>
            /// Gets the remote target feeds.
            /// (This is extracted as an independent function to be more easily transformable.)
            /// </summary>
            /// <returns></returns>
            public static IEnumerable<NuGetHelper.NuGetFeed> GetTargetRemoteFeeds(ICakeContext cake)
            {
                return new NuGetHelper.NuGetFeed[]{

new SignatureVSTSFeed(cake, "Signature-OpenSource", "Default" )
};
            }

            /// <summary>
            /// Call a transformed method. Keep these methods separated.
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<ArtifactFeed> GetTargetRemoteFeeds() => GetTargetRemoteFeeds(Cake);

            public override IEnumerable<ArtifactFeed> GetLocalFeeds()
            {
                return new NuGetHelper.NuGetFeed[] {
                    new NugetLocalFeed(Cake, CheckRepositoryInfo.LocalFeedPath )
                };
            }

            readonly IDictionary<string, ArtifactInstance> _resolved;

            protected override IDictionary<string, ArtifactInstance> ArtifactResolver( IEnumerable<string> projectsToPublish )
            {
                return _resolved;
            }
            public NuGetRepositoryInfo(
                ICakeContext ctx,
                CheckRepositoryInfo checkInfo,
                IEnumerable<SolutionProject> projectsToPublish )
                : base( ctx, checkInfo, projectsToPublish.Select( p => p.Path.FullPath ) )
            {
                _resolved = projectsToPublish.ToDictionary(
                    p => p.Path.FullPath,
                    p => new ArtifactInstance(
                        "NuGet",
                        p.Name,
                        CheckRepositoryInfo.Version
                    )
                 );
            }
            public string BuildConfiguration => CheckRepositoryInfo.IsRelease ? "Release" : "Debug";
        }
    }
}
