using Cake.Core;
using Cake.Core.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CodeCake.Build;
using Cake.Common.Diagnostics;

namespace CodeCake.Abstractions
{
    public abstract class ArtifactRepository
    {
        readonly IEnumerable<string> _projectsToPublish;
        protected ArtifactRepository( ICakeContext ctx, CheckRepositoryInfo checkInfo, IEnumerable<string> projectsToPublish )
        {
            Cake = ctx;
            CheckRepositoryInfo = checkInfo;
            _projectsToPublish = projectsToPublish;
        }
        /// <summary>
        /// Called automatically by <see cref="Build.CheckRepositoryInfo"/> when you call <see cref="CheckRepositoryInfo.AddAndInitRepository(ArtifactRepository)"/>
        /// </summary>
        public void Init()
        {
            if( CheckRepositoryInfo.Version.IsValid )
            {
                ArtifactsToPublish = (IReadOnlyDictionary<string, ArtifactInstance>)ArtifactResolver( _projectsToPublish );
            }
            if( CheckRepositoryInfo.LocalFeedPath != null )
            {
                ArtifactFeed[] localFeeds = GetLocalFeeds().ToArray();
                foreach( var f in localFeeds )
                {
                    Cake.Information( $"Adding local feed {f.Name}: {CheckRepositoryInfo.LocalFeedPath}" );
                }
                Feeds.AddRange( localFeeds );
            }
            if( CheckRepositoryInfo.PushToRemote )
            {
                ArtifactFeed[] remoteFeeds = GetTargetRemoteFeeds().ToArray();
                foreach( ArtifactFeed f in remoteFeeds )
                {
                    Cake.Information( $"Adding remote feed: {f.Name}" );
                }
                Feeds.AddRange( remoteFeeds );
            }

            // Now that Local/RemoteFeeds are selected, we can check the packages that already exist
            // in those feeds.
            var all = Feeds.Select( f => f.InitializeArtifactsToPublishAsync( ArtifactsToPublish ) );
            Task.WaitAll( all.ToArray() );
            foreach( var feed in Feeds )
            {
                Cake.Information( $"Will publish on feed {feed.Name}" );
                feed.Information( _projectsToPublish );
            }

            int nbPackagesToPublish = ActualArtifactsToPublish.Count();
            if( nbPackagesToPublish == 0 )
            {
                Cake.Information( $"No packages out of {_projectsToPublish.Count()} projects to publish." );
            }
            else
            {
                Cake.Information( $"Should actually publish {nbPackagesToPublish} out of {_projectsToPublish.Count()} projects with version = {CheckRepositoryInfo.GitInfo.SafeNuGetVersion}" );
            }
        }

        public void PushArtifacts( string releasesDir )
        {
            var all = Feeds.Select( feed => feed.PushArtifactsAsync( releasesDir ) );
            Task.WaitAll( all.ToArray() );
        }

        protected ICakeContext Cake { get; }

        /// <summary>
        /// Resolve artifact path to <see cref="ArtifactInstance"/>.
        /// Allow the <see cref="ArtifactRepository"/> to store the info he want. 
        /// </summary>
        /// <param name="projectsToPublish"></param>
        /// <returns></returns>
        protected abstract IDictionary<string, ArtifactInstance> ArtifactResolver( IEnumerable<string> projectsToPublish );
        //public abstract ArtifactRepository CheckRepository( CheckRepositoryInfo checkInfo, IEnumerable<string> packagesToPublish );
        /// <summary>
        /// Gets the remote target feeds.
        /// (This is extracted as an independent function to be more easily transformable.)
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<ArtifactFeed> GetTargetRemoteFeeds();
        /// <summary>
        /// Gets the local target feeds.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<ArtifactFeed> GetLocalFeeds();

        /// <summary>
        /// Gets the parent <see cref="CheckRepositoryInfo"/>. Is this really usefull ?
        /// </summary>
        public CheckRepositoryInfo CheckRepositoryInfo { get; }

        /// <summary>
        /// Gets the mutable list of all feeds to which artifacts should be pushed.
        /// </summary>
        public List<ArtifactFeed> Feeds { get; } = new List<ArtifactFeed>();

        /// <summary>
        /// Gets all the artifacts to publish.
        /// </summary>
        public IReadOnlyDictionary<string, ArtifactInstance> ArtifactsToPublish { get; private set; }

        /// <summary>
        /// Gets the actual artifacts to publish, without the one already on the Repository
        /// </summary>
        public IEnumerable<ArtifactInstance> ActualArtifactsToPublish => Feeds.SelectMany( f => f.ArtifactsToPublish.Values ).Distinct();
        /// <summary>
        /// Gets whether there is at least one artifact to produce and push.
        /// </summary>
        public bool NoArtifactsToProduce => !Feeds.SelectMany( f => f.ArtifactsToPublish ).Any();
    }
}
