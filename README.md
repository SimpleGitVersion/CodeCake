Code.Cake
=========

Code-based approach to the [Cake (C# Make)](https://cakebuild.net/) build system. Provides a C# build host to use [Cake methods](https://cakebuild.net/api/), utilities and addins in a .NET application.

This project is not affiliated with [Cake](https://github.com/cake-build/cake), nor supported by the Cake contributors.

Using Code.Cake
===============

1. Create or open a NET framework C# project
2. Install the [Code.Cake NuGet package](https://www.nuget.org/packages/Code.Cake/): `Install-Package Code.Cake`
3. Create a build host class:

```csharp
// Cake extension methods and utilities are split into many different namespaces, which all need to be specified.
// They're all on https://cakebuild.net/api/ if you're looking for them.
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.MSBuild;

// AddPathAttribute will add directories to the process' PATH environment variable.
// Use it if you have external executables like nuget.exe, octo.exe, etc.
[AddPath("packages/**/tools*")]
public class Build : CodeCakeHost
{
    public Build()
    {
        // The Cake property has all Cake properties, tools and extension methods on it
        Cake.Log.Verbosity = Verbosity.Diagnostic;

        var binDir = Cake.Directory("bin");
        var solutionFile = Cake.File("MySolution.sln");

        // Task example
        Task("Clean")
            .Does(() =>
            {
                Cake.CleanDirectories(binDir);
            });

        // Tasks can depend on other tasks, which will be executed before
        Task("Build")
            .IsDependentOn("Clean");
            .Does(() =>
            {
                Cake.NuGetRestore(solutionFile);
                Cake.MSBuild(solutionFile);
            });

        // If no task is specified when you execute Code.Cake, the "Default" task will be executed
        Task("Default")
            .IsDependentOn("Build");
    }
}
```

3. In your code, call `Code.Cake`;

```csharp
string[] cakeArgs;
var app = new CodeCakeApplication();
app.Run(cakeArgs);
```

## Build instructions

1. Clone the repository
2. Using Powershell, execute `CodeCakeBuilder/Bootstrap.ps1`
3. Execute `CodeCakeBuilder/bin/Release/CodeCakeBuilder.exe`

## NuGet packages

| Feed             | Code.Cake |
| ---------------- | ------ |
| **NuGet stable** | [![NuGet](https://img.shields.io/nuget/v/Code.Cake.svg)](https://www.nuget.org/packages/Code.Cake) |
| NuGet prerelease | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/Code.Cake.svg)](https://www.nuget.org/packages/Code.Cake) |
| MyGet preview    | [![MyGet Pre Release](https://img.shields.io/myget/invenietis-preview/vpre/Code.Cake.svg)](https://www.myget.org/feed/invenietis-preview/package/nuget/Code.Cake)  |
| MyGet CI         | [![MyGet Pre Release](https://img.shields.io/myget/invenietis-ci/vpre/Code.Cake.svg)](https://www.myget.org/feed/invenietis-ci/package/nuget/Code.Cake) |

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
