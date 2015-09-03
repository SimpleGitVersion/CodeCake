using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Common.Solution
{
    /// <summary>
    /// Internal implementation of <see cref="ITemporarySolutionFile"/>.
    /// Use <see cref="CodeCakeSolutionExtensions.CreateTemporarySolutionFile(ICakeContext, FilePath)">CreateTemporarySolutionFile</see> extension method to obtain a concrete implementation.
    /// </summary>
    class TemporarySolutionFile : ITemporarySolutionFile
    {
        readonly ICakeContext _cake;
        readonly FilePath _originalPath;
        readonly FilePath _modifiedPath;

        public TemporarySolutionFile( ICakeContext ctx, FilePath originalPath )
        {
            _cake = ctx;
            _originalPath = ctx.MakeAbsolute( originalPath );
            _modifiedPath = _originalPath + Guid.NewGuid().ToString( "N" ) + ".sln";
            ctx.CopyFile( _originalPath, _modifiedPath );
        }

        public ICakeContext Cake { get { return _cake; } }

        public FilePath OriginalFullPath { get { return _originalPath; } }

        public FilePath FullPath { get { return _modifiedPath; } }

        public void Dispose()
        {
            _cake.DeleteFile( _modifiedPath );
        }

        public void ExcludeProjectsFromBuild( params string[] projectNames )
        {
            ExcludeProjectsFromBuild( (IEnumerable<string>)projectNames );
        }

        public void ExcludeProjectsFromBuild( IEnumerable<string> projectNames )
        {
            var solution = Cake.ParseSolution( _modifiedPath );
            var toRemove = solution.Projects.Where( p => projectNames.Contains( p.Name ) );
            var lines = File.ReadAllLines( _modifiedPath.FullPath );
            File.WriteAllLines( _modifiedPath.FullPath, ProcessLines( lines, toRemove ) );
        }

        static IEnumerable<string> ProcessLines( IEnumerable<string> lines, IEnumerable<SolutionProject> projects )
        {
            bool inSection = false;
            foreach( var line in lines )
            {
                if( inSection )
                {
                    if( !projects.Any( p => line.Contains( p.Id ) ) ) yield return line;
                    inSection = !line.Contains( "EndGlobalSection" );
                }
                else
                {
                    yield return line;
                    inSection = line.Contains( "GlobalSection(ProjectConfigurationPlatforms) = postSolution" );
                }
            }
        }

    }
}
