using Cake.Common.Build;
using Cake.Common.Build.AppVeyor;
using Cake.Common.Build.TFBuild;
using Cake.Common.Diagnostics;
using SimpleGitVersion;
using System;

namespace CodeCake
{
    public partial class Build
    {
        void AppVeyorUpdateBuildVersion( IAppVeyorProvider appVeyor, SimpleRepositoryInfo gitInfo )
        {
            try
            {
                appVeyor.UpdateBuildVersion( gitInfo.SafeNuGetVersion );
            }
            catch
            {
                appVeyor.UpdateBuildVersion( $"{gitInfo.SafeNuGetVersion} ({appVeyor.Environment.Build.Number})" );
            }
        }

        void AzurePipelineUpdateBuildVersion( SimpleRepositoryInfo gitInfo )
        {
            // Azure (formerly VSTS, formerly VSO) analyzes the stdout to set its build number.
            // On clash, the default Azure/VSTS/VSO build number is used: to ensure that the actual
            // version will be always be available we need to inject a uniquifier.
            string buildVersion = $"{gitInfo.SafeNuGetVersion}_{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            Cake.Information( $"Using VSTS build number: {buildVersion}" );
            string buildInstruction = $"##vso[build.updatebuildnumber]{buildVersion}";
            Console.WriteLine();
            Console.WriteLine( buildInstruction );
            Console.WriteLine();
        }

        void SetCIVersionOnRunner( CheckRepositoryInfo checkInfo )//put this in the teardown.
        {
            IAppVeyorProvider appVeyor = Cake.AppVeyor();
            if( appVeyor.IsRunningOnAppVeyor )
            {
                AppVeyorUpdateBuildVersion( appVeyor, checkInfo.GitInfo );
            }
            var gitlab = Cake.GitLabCI();
            if( gitlab.IsRunningOnGitLabCI )
            {
                //damned, we can't tag the pipeline/job
            }
            ITFBuildProvider vsts = Cake.TFBuild();
            if( vsts.IsRunningOnAzurePipelinesHosted || vsts.IsRunningOnAzurePipelines )
            {
                AzurePipelineUpdateBuildVersion( checkInfo.GitInfo );
            }
        }
    }
}
