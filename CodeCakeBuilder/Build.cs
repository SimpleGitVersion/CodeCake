using Cake.Common;
using Cake.Common.Solution;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Core;
using Cake.Common.Diagnostics;
using Code.Cake;
using Cake.Common.Tools.NuGet.Pack;
using System.Linq;
using Cake.Core.Diagnostics;
using Cake.Common.Tools.NuGet.Restore;
using System;
using Cake.Common.Tools.NuGet.Push;
using SimpleGitVersion;
using Cake.Common.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Common.Build.AppVeyor;
using Cake.Common.Build;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;

namespace CodeCake
{
    /// <summary>
    /// CodeCakeBuilder for Code.Cake.
    /// </summary>
    [AddPath( "%UserProfile%/.nuget/packages/**/tools*" )]
    // These dynamic paths are used to test the dynamic path feature itself.
    // This is the default (starting with version v0.8.0).
    [AddPath( "CodeCakeBuilder/**/TestDynamic?", isDynamicPath: true )]
    [AddPath( "CodeCakeBuilder/AutoTests", isDynamicPath: true )]
    public partial class Build : CodeCakeHost
    {
        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            const string solutionName = "CodeCake";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );
            Cake.CreateDirectory( releasesDir );

            var projects = Cake.ParseSolution( solutionFileName )
                                       .Projects
                                       .Where( p => !(p is SolutionFolder)
                                                    && p.Name != "CodeCakeBuilder" );

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects.Where( p => !p.Path.Segments.Contains( "Tests" ) );

            // The SimpleRepositoryInfo should be computed once and only once.
            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();
            // This default global info will be replaced by Check-Repository task.
            // It is allocated here to ease debugging and/or manual work on complex build script.
            CheckRepositoryInfo globalInfo = new CheckRepositoryInfo( gitInfo, projectsToPublish );

            Setup( context =>
            {
                context.Information( "Executed BEFORE the first task." );
            } );

            Teardown( context =>
            {
                context.Information( "Executed AFTER the last task." );
            } );

            TaskSetup( setupContext =>
            {
                setupContext.Information( $"TaskSetup for Task: {setupContext.Task.Name}" );
            } );

            TaskTeardown( teardownContext =>
            {
                teardownContext.Information( $"TaskTeardown for Task: {teardownContext.Task.Name}" );
            } );

            Task( "Check-Repository" )
                .Does( () =>
                {
                    globalInfo = StandardCheckRepository( projectsToPublish, gitInfo );
                    if( globalInfo.ShouldStop )
                    {
                        Cake.TerminateWithSuccess( "All packages from this commit are already available. Build skipped." );
                    }
                } );

            Task( "Clean" )
                .Does( () =>
                {
                    Cake.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
                    Cake.CleanDirectories( releasesDir );
                } );

            // Use N as the first answser: this test takes a looong time (why?)
            // In -autointeraction mode, this will be skipped (unless explicitly asked from the command line).
            Task( "AutoTests" )
               .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                                    || Cake.ReadInteractiveOption( "RunAutoTests", "Run Auto Tests (for Dynamic paths)?", 'N', 'Y' ) == 'Y')
               .Does( () =>
               {
                   void ShouldFindAutoTestFolderFromDynamicPaths( bool shouldFind )
                   {
                       string[] paths = Cake.Environment.GetEnvironmentVariable( "PATH" ).Split( new char[] { Cake.Environment.Platform.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries );
                       // Cake does not normalize the paths to System.IO.Path.DirectorySeparatorChar. We do it here.
                       string af = Cake.Environment.WorkingDirectory.FullPath + "/CodeCakeBuilder/AutoTests".Replace( '\\', '/' );
                       bool autoFolder = paths.Select( p => p.Replace( '\\', '/' ) ).Contains( af );
                       if( autoFolder != shouldFind ) throw new Exception( shouldFind ? "AutoTests folder should be found." : "AutoTests folder should not be found." );
                   }

                   void ShouldFindTestTxtFileFromDynamicPaths( bool shouldFind )
                   {
                       string[] paths = Cake.Environment.GetEnvironmentVariable( "PATH" ).Split( new char[] { Cake.Environment.Platform.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries );
                       bool findTestTxtFileInPath = paths.Select( p => System.IO.Path.Combine( p, "Test.txt" ) ).Any( f => System.IO.File.Exists( f ) );
                       if( findTestTxtFileInPath != shouldFind ) throw new Exception( shouldFind ? "Should find Text.txt file." : "Should not find Test.txt file." );
                   }

                   if( System.IO.Directory.Exists( "CodeCakeBuilder/AutoTests" ) )
                   {
                       Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests", new DeleteDirectorySettings() { Recursive = true, Force = true } );
                   }
                   ShouldFindAutoTestFolderFromDynamicPaths( false );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   Cake.CreateDirectory( "CodeCakeBuilder/AutoTests/TestDynamic0" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   System.IO.File.WriteAllText( "CodeCakeBuilder/AutoTests/TestDynamic0/Test.txt", "c" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( true );
                   Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests/TestDynamic0", new DeleteDirectorySettings() { Recursive = true, Force = true } );
                   Cake.CreateDirectory( "CodeCakeBuilder/AutoTests/Sub/TestDynamicB" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   System.IO.File.WriteAllText( "CodeCakeBuilder/AutoTests/Sub/TestDynamicB/Test.txt", "c" );
                   ShouldFindTestTxtFileFromDynamicPaths( true );
                   Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests", new DeleteDirectorySettings() { Recursive = true, Force = true } );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   ShouldFindAutoTestFolderFromDynamicPaths( false );
               } );

            Task( "Build" )
                .IsDependentOn( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "AutoTests" )
                .Does( () =>
                {
                    StandardSolutionBuild( solutionFileName, gitInfo, globalInfo.BuildConfiguration );
                } );

            Task( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Build" )
                .Does( () =>
                {
                    StandardCreateNuGetPackages( releasesDir, projectsToPublish, gitInfo, globalInfo.BuildConfiguration );
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
               .Does( () =>
                {
                    StandardPushNuGetPackages( globalInfo, releasesDir );
                } );

            Task( "Default" ).IsDependentOn( "Push-NuGet-Packages" );

        }
    }
}
