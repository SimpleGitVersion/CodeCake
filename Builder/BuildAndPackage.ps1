# You may move this bootstrap.ps1 to the solution directory or let it in Builder folder:
# The $solutionDir and $builderDir are automatically adapted.
#
$solutionDir = $PSScriptRoot
$builderDir = Join-Path $solutionDir "Builder"
if (!(Test-Path $builderDir -PathType Container)) {
    $builderDir = $PSScriptRoot
    $solutionDir = Join-Path $builderDir ".."
}

$sln = Join-Path $solutionDir "Code.Cake.sln"
$nuspecFile = Join-Path $builderDir "Code.Cake.nuspec"
$packageFile = Join-Path $solutionDir "Code.Cake\packages.config"
[xml]$xmlPackage = Get-Content $packageFile 
$cakeVersion = $xmlPackage.packages.package.version | Select-Object -first 1

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



Push-Location $solutionDir
&$nugetExe restore
&$msbuildExe /p:Configuration=Release
Pop-Location

&$nugetExe pack $nuspecFile -Version $cakeVersion


