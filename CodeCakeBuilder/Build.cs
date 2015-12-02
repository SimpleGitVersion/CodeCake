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
    [AddPath( "CodeCakeBuilder/Tools" )]
    [AddPath( "packages/**/tools*" )]
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
                    if( gitInfo.IsValid )
                    {
                        configuration = gitInfo.IsValidRelease && gitInfo.PreReleaseName.Length == 0 ? "Release" : "Debug";
                        Cake.Information( "Publishing {0} in {1}.", gitInfo.SemVer, configuration );
                    }
                    else
                    {
                        configuration = "Debug";
                        Cake.Warning( "Repository is not ready to be published. Selecting Debug configuration." );
                    }
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

            Task( "Build" )
                .IsDependentOn( "Clean" )
                .IsDependentOn( "Restore-NuGet-Packages" )
                .IsDependentOn( "Check-Repository" )
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
                        PushNuGetPackages( "NUGET_API_KEY", "https://www.nuget.org/api/v2/package", nugetPackages );
                    }
                    else
                    {
                        Debug.Assert( gitInfo.IsValidCIBuild );
                        PushNuGetPackages( "MYGET_EXPLORE_API_KEY", "https://www.myget.org/F/invenietis-explore/api/v2/package", nugetPackages );
                    }
                } );

            Task( "Default" ).IsDependentOn( "Push-NuGet-Packages" );

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
