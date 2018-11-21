using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Core;
using Cake.Core.IO;
using SimpleGitVersion;
using System.Collections.Generic;

namespace CodeCake
{
    public partial class Build
    {
        void StandardCreateNuGetPackages( DirectoryPath releasesDir, IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo, string configuration )
        {
            var settings = new DotNetCorePackSettings().AddVersionArguments( gitInfo, c =>
            {
                // IsPackable=true is required for Tests package. Without this Pack on Tests projects
                // does not generate nupkg.
                c.ArgumentCustomization += args => args.Append( "/p:IsPackable=true" );
                c.NoBuild = true;
                c.IncludeSymbols = true;
                c.Configuration = configuration;
                c.OutputDirectory = releasesDir;
            } );
            foreach( SolutionProject p in projectsToPublish )
            {
                Cake.Information( p.Path.GetDirectory().FullPath );
                Cake.DotNetCorePack( p.Path.GetDirectory().FullPath, settings );
            }
        }
    }
}
