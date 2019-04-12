using Cake.Common.Diagnostics;
using Cake.Core;
using CK.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCake.Abstractions
{
    public abstract class ArtifactFeed
    {
        public ArtifactFeed( ICakeContext cake )
        {
            Cake = cake;
        }
        protected ICakeContext Cake { get; }
        /// <summary>
        /// Initializes the <see cref="ArtifactsToPublish"/> and <see cref="ArtifactsAlreadyPublishedCount"/>.
        /// This can be called multiple times.
        /// </summary>
        /// <param name="allArtifactsToPublish"></param>
        /// <returns></returns>
        public abstract Task InitializeArtifactsToPublishAsync( IReadOnlyDictionary<string, ArtifactInstance> allArtifactsToPublish );
        /// <summary>
        /// Gets the number of artifacts that already exist in the feed.
        /// </summary>
        public int ArtifactsAlreadyPublishedCount { get; protected set; }
        /// <summary>
        /// Gets the list of artifacts that must be published (ie. they don't already exist in the feed)
        /// </summary>
        public IReadOnlyDictionary<string, ArtifactInstance> ArtifactsToPublish { get; protected set; }
        /// <summary>
        /// Pushes a set of artifcats (this is typically <see cref="ArtifactsToPublish"/>).
        /// </summary>
        /// <param name="artifactsFolder">Folder where the packed artifacts are ready to be published</param>
        /// <returns>The awaitable.</returns>
        public abstract Task PushArtifactsAsync( string artifactsFolder );
        /// <summary>
        /// Name used to print the feed in the logs.
        /// It's a good idea to put the URL here.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Dumps information about <see cref="PackagesToPublish"/>.
        /// </summary>
        /// <param name="ctx">The Cake context.</param>
        /// <param name="allPackagesToPublish">The set of all packages to publish.</param>
        public void Information( IEnumerable<string> allPackagesToPublish )
        {
            if( ArtifactsToPublish.Count == 0 )
            {
                Cake.Information( $"Feed '{Name}': No packages must be pushed ({ArtifactsAlreadyPublishedCount} packages already available)." );
            }
            else if( ArtifactsAlreadyPublishedCount == 0 )
            {
                Cake.Information( $"Feed '{Name}': All {ArtifactsToPublish.Count} packages must be pushed." );
            }
            else
            {
                Cake.Information( $"Feed '{Name}': {ArtifactsToPublish.Count} packages must be pushed: {ArtifactsToPublish.Select( p => p.Value.Artifact.Name ).Concatenate()}." );
                Cake.Information( $"               => {ArtifactsAlreadyPublishedCount} packages already pushed: {allPackagesToPublish.Except( ArtifactsToPublish.Keys ).Concatenate()}." );
            }
        }
    }
}
