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

namespace CodeCake
{
    /// <summary>
    /// CodeCakeBuilder for Code.Cake.
    /// </summary>
    [AddPath( "CodeCakeBuilder/Tools", isDynamicPath: false )]
    [AddPath( "packages/**/tools*", isDynamicPath: false )]
    // These dynamic paths are used to test the dynamic path feature itself.
    // This is the default (starting with version v0.8.0).
    [AddPath( "CodeCakeBuilder/**/TestDynamic?", isDynamicPath: true )]
    [AddPath( "CodeCakeBuilder/AutoTests", isDynamicPath: true )]
    public class Build : CodeCakeHost
    {
        public Build()
        {
            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );
            string configuration = null;
            SimpleRepositoryInfo gitInfo = null;

            Task( "Check-Repository" )
                .Does( () =>
                {
                    gitInfo = Cake.GetSimpleRepositoryInfo();
                    if( !gitInfo.IsValid )
                    {
                        if( Cake.IsInteractiveMode()
                            && Cake.ReadInteractiveOption( "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                        {
                            Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                        }
                        else throw new Exception( "Repository is not ready to be published." );
                    }
                    configuration = gitInfo.IsValidRelease && gitInfo.PreReleaseName.Length == 0 ? "Release" : "Debug";
                } );

            Task( "Clean" )
                .Does( () =>
                {
                    // Avoids cleaning CodeCakeBuilder itself!
                    Cake.CleanDirectories( "**/bin/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
                    Cake.CleanDirectories( "**/obj/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
                    Cake.CleanDirectories( releasesDir );
                } );

            Task( "Restore-NuGet-Packages" )
                .Does( () =>
                {
                    Cake.NuGetRestore( "CodeCake.sln" );
                } );

            Task( "AutoTests" )
               .Does( () =>
               {
                   if( System.IO.Directory.Exists( "CodeCakeBuilder/AutoTests" ) ) Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests", true );

                   ShouldFindAutoTestFolderFromDynamicPaths( false );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   Cake.CreateDirectory( "CodeCakeBuilder/AutoTests/TestDynamic0" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   System.IO.File.WriteAllText( "CodeCakeBuilder/AutoTests/TestDynamic0/Test.txt", "c" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( true );
                   Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests/TestDynamic0", true );
                   Cake.CreateDirectory( "CodeCakeBuilder/AutoTests/Sub/TestDynamicB" );
                   ShouldFindAutoTestFolderFromDynamicPaths( true );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   System.IO.File.WriteAllText( "CodeCakeBuilder/AutoTests/Sub/TestDynamicB/Test.txt", "c" );
                   ShouldFindTestTxtFileFromDynamicPaths( true );
                   Cake.DeleteDirectory( "CodeCakeBuilder/AutoTests", true );
                   ShouldFindTestTxtFileFromDynamicPaths( false );
                   ShouldFindAutoTestFolderFromDynamicPaths( false );
               } );

            Task( "Build" )
                .IsDependentOn( "Clean" )
                .IsDependentOn( "Restore-NuGet-Packages" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "AutoTests" )
                .Does( () =>
                {
                    Cake.Information( "Building CodeCake.sln with '{0}' configuration (excluding this builder application).", configuration );
                    using( var tempSln = Cake.CreateTemporarySolutionFile( "CodeCake.sln" ) )
                    {
                        tempSln.ExcludeProjectsFromBuild( "CodeCakeBuilder" );
                        Cake.MSBuild( tempSln.FullPath, new MSBuildSettings()
                                .SetConfiguration( configuration )
                                .SetVerbosity( Verbosity.Minimal )
                                .SetMaxCpuCount( 1 )
                                // Always generates Xml documentation. Relies on this definition in the csproj files:
                                //
                                // <PropertyGroup Condition=" $(GenerateDocumentation) != '' ">
                                //   <DocumentationFile>bin\$(Configuration)\$(AssemblyName).xml</DocumentationFile>
                                // </PropertyGroup>
                                //
                                .WithProperty( "GenerateDocumentation", new[] { "true" } )
                            );
                    }
                } );

            Task( "Create-NuGet-Packages" )
                    .IsDependentOn( "Build" )
                    .WithCriteria( () => gitInfo.IsValid )
                    .Does( () =>
                    {
                        Cake.CreateDirectory( releasesDir );
                        var settings = new NuGetPackSettings()
                        {
                            Version = gitInfo.NuGetVersion,
                            BasePath = Cake.Environment.WorkingDirectory,
                            OutputDirectory = releasesDir
                        };
                        Cake.CopyFiles( "CodeCakeBuilder/NuSpec/*.nuspec", releasesDir );
                        foreach( var nuspec in Cake.GetFiles( releasesDir.Path + "/*.nuspec" ) )
                        {
                            Cake.TransformTextFile( nuspec, "{{", "}}" )
                                    .WithToken( "configuration", configuration )
                                    .WithToken( "CSemVer", gitInfo.SemVer )
                                    .Save( nuspec );
                            Cake.NuGetPack( nuspec, settings );
                        }
                        Cake.DeleteFiles( releasesDir.Path + "/*.nuspec" );
                    } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                {
                    IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
                    if( Cake.IsInteractiveMode() )
                    {
                        var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                        if( localFeed != null )
                        {
                            Cake.Information( "LocalFeed directory found: {0}", localFeed );
                            if( Cake.ReadInteractiveOption( "Do you want to publish to LocalFeed?", 'Y', 'N' ) == 'Y' )
                            {
                                Cake.CopyFiles( nugetPackages, localFeed );
                            }
                        }
                    }
                    if( gitInfo.IsValidRelease )
                    {
                        if( gitInfo.PreReleaseName == ""
                            || gitInfo.PreReleaseName == "prerelease"
                            || gitInfo.PreReleaseName == "rc" )
                        {
                            PushNuGetPackages( "NUGET_API_KEY", "https://www.nuget.org/api/v2/package", nugetPackages );
                        }
                        else
                        {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                            PushNuGetPackages( "MYGET_PREVIEW_API_KEY", "https://www.myget.org/F/invenietis-preview/api/v2/package", nugetPackages );
                        }
                    }
                    else
                    {
                        Debug.Assert( gitInfo.IsValidCIBuild );
                        PushNuGetPackages( "MYGET_CI_API_KEY", "https://www.myget.org/F/invenietis-ci/api/v2/package", nugetPackages );
                    }
                } );

            Task( "Default" ).IsDependentOn( "Push-NuGet-Packages" );

        }

        private void ShouldFindAutoTestFolderFromDynamicPaths( bool shouldFind )
        {
            string[] paths = Cake.Environment.GetEnvironmentVariable( "PATH" ).Split( new char[] { Machine.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries );
            // Cake does not normalize the paths to System.IO.Path.DirectorySeparatorChar. We do it here.
            string af = Cake.Environment.WorkingDirectory.FullPath + "/CodeCakeBuilder/AutoTests".Replace( '\\', '/' );
            bool autoFolder = paths.Select( p => p.Replace( '\\', '/' ) ).Contains( af );
            if( autoFolder != shouldFind ) throw new Exception( shouldFind ? "AutoTests folder should be found." : "AutoTests folder should not be found." );
        }

        private void ShouldFindTestTxtFileFromDynamicPaths( bool shouldFind )
        {
            string[] paths = Cake.Environment.GetEnvironmentVariable( "PATH" ).Split( new char[] { Machine.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries );
            bool findTestTxtFileInPath = paths.Select( p => System.IO.Path.Combine( p, "Test.txt" ) ).Any( f => System.IO.File.Exists( f ) );
            if( findTestTxtFileInPath != shouldFind ) throw new Exception( shouldFind ? "Should find Text.txt file." : "Should not find Test.txt file." );
        }

        private void PushNuGetPackages( string apiKeyName, string pushUrl, IEnumerable<FilePath> nugetPackages )
        {
            // Resolves the API key.
            var apiKey = Cake.InteractiveEnvironmentVariable( apiKeyName );
            if( string.IsNullOrEmpty( apiKey ) )
            {
                Cake.Information( "Could not resolve {0}. Push to {1} is skipped.", apiKeyName, pushUrl );
            }
            else
            {
                var settings = new NuGetPushSettings
                {
                    Source = pushUrl,
                    ApiKey = apiKey
                };

                foreach( var nupkg in nugetPackages )
                {
                    Cake.NuGetPush( nupkg, settings );
                }
            }
        }
    }
}
