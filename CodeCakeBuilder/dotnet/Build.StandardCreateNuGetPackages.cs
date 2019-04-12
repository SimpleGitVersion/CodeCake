using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Core;
using Cake.Core.IO;
using SimpleGitVersion;
using System;
using System.Collections.Generic;

namespace CodeCake
{
    public partial class Build
    {
        [Obsolete]
        void StandardCreateNuGetPackages( DirectoryPath releasesDir, IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo, NuGetRepositoryInfo nugetInfo )
        {
            StandardCreateNuGetPackages( nugetInfo, releasesDir );
        }

        void StandardCreateNuGetPackages( NuGetRepositoryInfo nugetInfo, DirectoryPath releasesDir )
        {
            var settings = new DotNetCorePackSettings().AddVersionArguments( nugetInfo.CheckRepositoryInfo.GitInfo, c =>
            {
                // IsPackable=true is required for Tests package. Without this Pack on Tests projects
                // does not generate nupkg.
                c.ArgumentCustomization += args => args.Append( "/p:IsPackable=true" );
                c.NoBuild = true;
                c.IncludeSymbols = true;
                c.Configuration = nugetInfo.BuildConfiguration;
                c.OutputDirectory = releasesDir;
            } );
            foreach( var p in nugetInfo.ArtifactsToPublish )
            {
                Cake.Information( p.Key );
                Cake.DotNetCorePack( p.Key, settings );
            }
        }
    }
}
