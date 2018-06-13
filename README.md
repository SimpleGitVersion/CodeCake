Code.Cake
=========

Code-based approach to the [Cake (C# Make)](https://cakebuild.net/) build system. Provides a C# build host to use [Cake methods](https://cakebuild.net/api/), utilities and addins in a .NET application.

This project is not affiliated with [Cake](https://github.com/cake-build/cake), nor supported by the Cake contributors.

Using Code.Cake
===============

1. Create or open a NET framework C# project
2. Install the [Code.Cake NuGet package](https://www.nuget.org/packages/Code.Cake/): `Install-Package Code.Cake`
3. Create a build class:

```csharp
// Cake extension methods and utilities are split into many different namespaces, which all need to be specified.
// They're all on https://cakebuild.net/api/ if you're looking for them.
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;

// AddPathAttribute will add directories to the process' PATH environment variable.
// Use it if you have external executables like nuget.exe, octo.exe, etc.
[AddPath("packages/**/tools*")]
[AddPath("%UserProfile%/.nuget/packages/**/tools*")]
public class Build : CodeCakeHost
{
    public Build()
    {
        // The Cake property has all Cake properties, tools and extension methods on it
        Cake.Log.Verbosity = Verbosity.Diagnostic;

        var configuration = "Debug";
        var solutionFileName = "MySolution.sln";

        // Task example
        Task("Clean")
            .Does(() =>
            {
                Cake.CleanDirectories( "**/bin/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
                Cake.CleanDirectories( "**/obj/" + configuration, d => !d.Path.Segments.Contains( "CodeCakeBuilder" ) );
            });

        // Tasks can depend on other tasks, which will be executed before
        Task("Build")
            .IsDependentOn("Clean");
            .Does(() =>
            {
                // CreateTemporarySolutionFile is a feature of Code.Cake.
                using( var tempSln = Cake.CreateTemporarySolutionFile( solutionFileName ) )
                {
                    tempSln.ExcludeProjectsFromBuild( "CodeCakeBuilder" );
                    Cake.DotNetCoreBuild( tempSln.FullPath.FullPath, new DotNetCoreBuildSettings()
                    {
                        Configuration = configuration,
                        Verbosity = DotNetCoreVerbosity.Minimal,
                        ArgumentCustomization = args => args.Append( "/p:GenerateDocumentation=true" )
                    } );
                }
            });

        // If no task is specified when you execute Code.Cake, the "Default" task will be executed
        Task("Default")
            .IsDependentOn("Build");
    }
}
```

3. In your code, call `Code.Cake`. Following Program.cs is the standard one we use: 

```csharp
using System;

namespace CodeCake
{
    class Program
    {
        /// <summary>
        /// CodeCakeBuilder entry point. This is a default, simple, implementation that can 
        /// be extended as needed.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>An error code (typically negative), 0 on success.</returns>
        static int Main( string[] args )
        {
            var app = new CodeCakeApplication();
            RunResult result = app.Run( args );
            if( result.InteractiveMode == InteractiveMode.Interactive )
            {
                Console.WriteLine();
                Console.WriteLine( $"Hit any key to exit." );
                Console.WriteLine( $"Use -{InteractiveAliases.NoInteractionArgument} or -{InteractiveAliases.AutoInteractionArgument} parameter to exit immediately." );
                Console.ReadKey();
            }
            return result.ReturnCode;

        }
    }
}
```
## More information: Multiple Build scripts, Interactive mode and Exclusive target
### Multiple Build scripts
You can have multiple Build class in a CodeCakeBuilder.exe.

```csharp
using Cake.Common.Diagnostics;
using Cake.Core.Diagnostics;

namespace CodeCake
{
    public class MySecondBuild : CodeCakeHost
    {
        public MySecondBuild()
        {
            Cake.Information( "I'm here!" );
        }
    }
}
```

Choose the one you want to run on the command line: `CodeCakeBuilder.exe MySecondBuild`

### Interactive Mode
By default CodeCake runs in interactive mode. This enables yor script to ask you questions like:

```csharp
    Task( "Run-IntegrationTests" )
        .IsDependentOn( "Compile-IntegrationTests" )
        .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                            || Cake.ReadInteractiveOption( "Run integration tests?", 'Y', 'N' ) == 'Y' )
        .Does( () =>
        {
            //...
        }
```

See the [InteractiveAliases source code](https://github.com/SimpleGitVersion/CodeCake/blob/master/Code.Cake/CodeCakeSpecific/InteractiveAliases.cs)
for more information about this CodeCake specific extension.

On CI server, or when no interaction are required, compile CodeCakBuilder project and launch: `CodeCakeBuilder.exe -nointeraction`, or even simpler from the solution directory just call `dotnet run --project CodeCakeBuilder -nointeraction`

An intermediate mode is available: `CodeCakeBuilder.exe -autointeraction`. In this mode, command line arguments
can drive the behavior of the execution. Given the sample script below:

```csharp
    IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
    if( Cake.InteractiveMode() != InteractiveMode.NoInteraction )
    {
        var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
        if( localFeed != null )
        {
            Cake.Information( "LocalFeed directory found: {0}", localFeed );
            if( Cake.ReadInteractiveOption( "PushLocal", "Do you want to publish to LocalFeed?", 'Y', 'N' ) == 'Y' )
            {
                Cake.CopyFiles( nugetPackages, localFeed );
            }
        }
    }
    var apiKey = Cake.InteractiveEnvironmentVariable( "NUGET_API_KEY" );
    if( string.IsNullOrEmpty( apiKey ) )
    {
        Cake.Information( "Could not resolve NUGET_API_KEY. Push to https://www.nuget.org/api/v2/package is skipped." );
    }
    else
    {
        var settings = new NuGetPushSettings
        {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = apiKey
        };
        foreach( var nupkg in nugetPackages ) Cake.NuGetPush( nupkg, settings );
    }
```

`CodeCakeBuilder.exe -autointeraction -PushLocal=N -ENV:NUGET_API_KEY="xxx"`

Will not push to the local feed but will try to push to the nuget feed.
With `-autointeraction`, when no command line argument can be found for the `ReadInteractiveOption`, the **first choice is assumed** (for
the LocalPush above, it would be **Y**[es]).

### Exclusive target and ExclusiveOptional target

CodeCake supports to run one and only one task, ignoring any of its dependencies. This is useful when you need an
external control of the Build script (when the dependent tasks have alreay been peformed and you don't want to pay the cost of running them again).

`CodeCakeBuilder.exe -nointeraction -target="Unit-Testing" -exclusive`

Will only run the Unit-Testing task but NOT its dependencies.

`CodeCakeBuilder.exe -nointeraction -target="Unit-Testing" -exclusiveOptional`

Will only run the Unit-Testing task (and NOT its dependencies) ONLY if it exists. If the task does not
exist, nothing is done and a warning is emitted:

`No task 'Unit-Testing' defined. Since -exclusiveOptional is specified, nothing is done.



## Build instructions

1. Clone the repository
2. Execute `dotnet run --project CodeCakeBuilder -nointeraction`

## NuGet packages

| Feed             | Code.Cake |
| ---------------- | ------ |
| **NuGet stable** | [![NuGet](https://img.shields.io/nuget/v/Code.Cake.svg)](https://www.nuget.org/packages/Code.Cake) |
| NuGet prerelease | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Code.Cake.svg)](https://www.nuget.org/packages/Code.Cake) |

## Build status

| Branch   | Visual Studio 2017 |
| -------- | ------- |
| latest | [![AppVeyor](https://img.shields.io/appveyor/ci/olivier-spinelli/codecake.svg)](https://ci.appveyor.com/project/olivier-spinelli/codecake) |
| `master` | [![AppVeyor](https://img.shields.io/appveyor/ci/olivier-spinelli/codecake/master.svg)](https://ci.appveyor.com/project/olivier-spinelli/codecake) |

## Contributing

Anyone and everyone is welcome to contribute. Please take a moment to
review the [guidelines for contributing](CONTRIBUTING.md).

## License

Assets in this repository are licensed with the MIT License. For more information, please see [LICENSE.txt](LICENSE.txt).

## Open-source licenses

This repository and its components use the following open-source projects:

- [cake-build/cake](https://github.com/cake-build/cake), licensed under the [MIT License](https://github.com/cake-build/cake/blob/develop/LICENSE)
- [SimpleGitVersion/SGV-Net](https://github.com/SimpleGitVersion/SGV-Net), licensed under the [MIT License](https://github.com/SimpleGitVersion/SGV-Net/blob/master/LICENSE.txt)
