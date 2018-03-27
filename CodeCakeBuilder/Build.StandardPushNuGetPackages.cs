using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Push;
using Cake.Core.IO;
using SimpleGitVersion;
using System.Collections.Generic;
using System.Diagnostics;

namespace CodeCake
{
    public partial class Build
    {

        void StandardPushNuGetPackages( IEnumerable<FilePath> nugetPackages, SimpleRepositoryInfo gitInfo )
        {
            if( Cake.InteractiveMode() != InteractiveMode.NoInteraction )
            {
                var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                if( localFeed != null )
                {
                    Cake.Information( $"LocalFeed directory found: {localFeed}" );
                    if( Cake.ReadInteractiveOption( "LocalFeed", "Do you want to publish to LocalFeed?", 'Y', 'N' ) == 'Y' )
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
                    PushNuGetPackages( "MYGET_RELEASE_API_KEY",
                                        "https://www.myget.org/F/invenietis-release/api/v2/package",
                                        "https://www.myget.org/F/invenietis-release/symbols/api/v2/package" );
                }
                else
                {
                    // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                    PushNuGetPackages( "MYGET_PREVIEW_API_KEY",
                                        "https://www.myget.org/F/invenietis-preview/api/v2/package",
                                        "https://www.myget.org/F/invenietis-preview/symbols/api/v2/package" );
                }
            }
            else
            {
                Debug.Assert( gitInfo.IsValidCIBuild );
                PushNuGetPackages( "MYGET_CI_API_KEY",
                                    "https://www.myget.org/F/invenietis-ci/api/v2/package",
                                    "https://www.myget.org/F/invenietis-ci/symbols/api/v2/package" );
            }
            if( Cake.AppVeyor().IsRunningOnAppVeyor )
            {
                Cake.AppVeyor().UpdateBuildVersion( gitInfo.SafeNuGetVersion );
            }

            void PushNuGetPackages( string apiKeyName, string pushUrl, string pushSymbolUrl )
            {
                // Resolves the API key.
                var apiKey = Cake.InteractiveEnvironmentVariable( apiKeyName );
                if( string.IsNullOrEmpty( apiKey ) )
                {
                    Cake.Information( $"Could not resolve {apiKeyName}. Push to {pushUrl} is skipped." );
                }
                else
                {
                    var settings = new NuGetPushSettings
                    {
                        Source = pushUrl,
                        ApiKey = apiKey,
                        Verbosity = NuGetVerbosity.Detailed
                    };
                    NuGetPushSettings symbSettings = null;
                    if( pushSymbolUrl != null )
                    {
                        symbSettings = new NuGetPushSettings
                        {
                            Source = pushSymbolUrl,
                            ApiKey = apiKey,
                            Verbosity = NuGetVerbosity.Detailed
                        };
                    }
                    foreach( var nupkg in nugetPackages )
                    {
                        if( !nupkg.FullPath.EndsWith( ".symbols.nupkg" ) )
                        {
                            Cake.Information( $"Pushing '{nupkg}' to '{pushUrl}'." );
                            Cake.NuGetPush( nupkg, settings );
                        }
                        else
                        {
                            if( symbSettings != null )
                            {
                                Cake.Information( $"Pushing Symbols '{nupkg}' to '{pushSymbolUrl}'." );
                                Cake.NuGetPush( nupkg, symbSettings );
                            }
                        }
                    }
                }
            }
        }

    }
}
