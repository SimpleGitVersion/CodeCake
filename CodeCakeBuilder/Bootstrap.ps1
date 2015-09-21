# This script builds CodeCakeBuilder with the help of nuget.exe (in Tools/, downloaded if missing)
# and MSBuild.
#
# You may move this bootstrap.ps1 to the solution directory or let it in CodeCakeBuilder folder:
# The $solutionDir and $builderDir are automatically adapted.
#
$solutionDir = $PSScriptRoot
$builderDir = Join-Path $solutionDir "CodeCakeBuilder"
if (!(Test-Path $builderDir -PathType Container)) {
    $builderDir = $PSScriptRoot
    $solutionDir = Join-Path $builderDir ".."
}

# Ensures that CodeCakeBuilder project exists.
$builderProj = Join-Path $builderDir "CodeCakeBuilder.csproj"
if (!(Test-Path $builderProj)) {
    Throw "Could not find CodeCakeBuilder.csproj"
}
# Ensures that packages.config file exists.
$builderPackageConfig = Join-Path $builderDir "packages.config"
if (!(Test-Path $builderPackageConfig)) {
    Throw "Could not find packages.config"
}

# Find MSBuild 4.0.
$dotNetVersion = "4.0"
$regKey = "HKLM:\software\Microsoft\MSBuild\ToolsVersions\$dotNetVersion"
$regProperty = "MSBuildToolsPath"
$msbuildExe = join-path -path (Get-ItemProperty $regKey).$regProperty -childpath "msbuild.exe"
if (!(Test-Path $msbuildExe)) {
    Throw "Could not find msbuild.exe"
}


# Tools directory is for nuget.exe but it may be used to 
# contain other utilities.
$toolsDir = Join-Path $builderDir "Tools"
if (!(Test-Path $toolsDir)) {
    New-Item -ItemType Directory $toolsDir | Out-Null
}

# Try download NuGet.exe if do not exist.
$nugetExe = Join-Path $toolsDir "nuget.exe"
if (!(Test-Path $nugetExe)) {
    Invoke-WebRequest -Uri http://nuget.org/nuget.exe -OutFile $nugetExe
    # Make sure NuGet it worked.
    if (!(Test-Path $nugetExe)) {
        Throw "Could not find NuGet.exe"
    }
}

&$nugetExe restore $builderPackageConfig -SolutionDirectory $solutionDir
&$msbuildExe $builderProj /p:Configuration=Release

