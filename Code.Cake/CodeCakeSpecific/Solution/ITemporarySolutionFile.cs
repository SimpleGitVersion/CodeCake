using System.Collections.Generic;
using Cake.Core;
using Cake.Core.IO;
using System;

namespace Cake.Common.Solution
{
    /// <summary>
    /// Disposable temporary solution file (.sln) that supports exclusion of projects from build.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We must be able to build a solution without the CodeCakeBuilder project (or any other projects).
    /// The first solution would be to explicitely compile each project... but there is currently no way 
    /// to obtain the projects in a list ordered by "Build Order".
    /// </para>
    /// <para>
    /// To compute it, one would need:
    /// </para>
    /// <para>
    /// - SolutionProject should expose a IEnumerable of SolutionProject (a ProjectReferences property).
    /// </para>
    /// <para>
    /// - Parsing of sln files should extract project dependencies section for each project.
    ///   This is required in order to honor explicit (reference independent) build ordering (Menu > Project > Project Build Order).
    /// 
    /// <code>
    ///   Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CodeCakeBuilder", "CodeCakeBuilder\CodeCakeBuilder.csproj", "{FD4817B6-3CD7-4E74-AA10-7CA95FDFCF2D}"
    ///     ProjectSection( ProjectDependencies ) = postProject
    ///       { 6D0D47C8 - 98F7 - 47DE - B118 - AD3606455F7E} = { 6D0D47C8 - 98F7 - 47DE - B118 - AD3606455F7E}
    ///     EndProjectSection
    ///   EndProject
    /// </code>
    ///  </para>
    /// <para>
    /// We should then order the graph... it is quite complex and we'll loose any solution peculiarities that may exist.
    /// </para>
    /// <para>
    /// The idea is to base the build on a modified sln and this is not so complex: we must just have to remove 
    /// all entries starting with the GUID of the project we don't want to build in the GlobalSection(ProjectConfigurationPlatforms):
    /// </para>
    /// <code>
    ///      GlobalSection(ProjectConfigurationPlatforms) = postSolution
    ///          ...
    ///          {FD4817B6-3CD7-4E74-AA10-7CA95FDFCF2D}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
    ///          ...
    ///      EndGlobalSection
    /// </code>
    /// <para>
    /// The default implementation is simple but works well.
    /// </para>
    /// </remarks>
    public interface ITemporarySolutionFile : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ICakeContext"/>.
        /// </summary>
        ICakeContext Cake { get; }

        /// <summary>
        /// Gets the full path of this temporary solution.
        /// </summary>
        FilePath FullPath { get; }

        /// <summary>
        /// Gets the original full path of solution.
        /// </summary>
        FilePath OriginalFullPath { get; }

        /// <summary>
        /// Excludes project from build by their names.
        /// This immediately updates the <see cref="FullPath"/> sln file.
        /// </summary>
        /// <param name="projectNames">Names of the projects to exclude from build.</param>
        void ExcludeProjectsFromBuild( params string[] projectNames );

        /// <summary>
        /// Excludes project from build by their names. This updates the <see cref="FullPath"/> sln file.
        /// </summary>
        /// <param name="projectNames">Names of the projects to exclude from build.</param>
        void ExcludeProjectsFromBuild( IEnumerable<string> projectNames );
    }
}