using DiagToolsValidationRunner.Core.Configuration.LTTngTest;
using DiagToolsValidationRunner.Core.Functionality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagToolsValidationRunner.Core.TestRunner.LTTngTest
{
    public class LTTngTestRunner
    {
        private readonly LTTngTestRunConfiguration RunConfig;

        public LTTngTestRunner(LTTngTestRunConfiguration runConfig,
                               string baseGCPerfsimAppSrcPath)
        {
            if (!OperatingSystem.IsLinux())
            {
                throw new Exception($"{nameof(LTTngTestRunner)}: LTTng test only runs on Linux");
            }
            RunConfig = runConfig;
            // Initialize test
            string initLoggerPath = Path.Combine(runConfig.Test.TestResultFolder,
                                                 $"Initialization-{runConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner.Run(initLoggerPath,
                                        this.InitializeTest(runConfig, baseGCPerfsimAppSrcPath));
        }

        private IEnumerable<CommandInvokeResult> InitializeTest(LTTngTestRunConfiguration runConfig,
                                                                string baseGCPerfsimAppSrcPath)
        {
            // Create analysis output folder and dump folder
            Directory.CreateDirectory(runConfig.Test.TestResultFolder);

            // Generate environment activation script
            string scriptPath = Path.Combine(runConfig.Test.TestBed,
                                             $"env_activation-sdk{runConfig.SDKSetting.Version}");
            DotNetInfrastructure.GenerateEnvironmentActivationScript(DotNetInfrastructure.CurrentRID,
                                                                     scriptPath,
                                                                     runConfig.SDKSetting.DotNetRoot);
            
            // Install .NET SDK
            Console.WriteLine($"Install .NET SDK {runConfig.SDKSetting.Version}");
            DotNetInfrastructure.InstallDotNetSDKByVersion(runConfig.SDKSetting.Version,
                                                           DotNetInfrastructure.CurrentRID,
                                                           runConfig.SDKSetting.DotNetRoot);

            Console.WriteLine($"Prepare .NET apps for testing({runConfig.SDKSetting.Version})");
            // 1. Create gcperfsim
            yield return RunConfig.AppSetting.GCPerfsim.CreateApp();

            // 2. Replace source file
            string targetgcperfsimSrcPath = Path.Combine(RunConfig.AppSetting.GCPerfsim.AppRoot, "Program.cs");
            string gcperfsimSrcContent = File.ReadAllText(baseGCPerfsimAppSrcPath);
            File.WriteAllText(targetgcperfsimSrcPath, gcperfsimSrcContent);

            // 3. Build gcperfsim
            yield return RunConfig.AppSetting.GCPerfsim.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                                 DotNetInfrastructure.CurrentRID);
        }

        public void TestLTTng(PerfCollect perfcollect)
        {
            // Active Tracing Environment
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);
            DotNetInfrastructure.ActiveTracingEnvironment(env);

            string gcperfsimExecutablePath =
                RunConfig.AppSetting.GCPerfsim.GetAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                DotNetInfrastructure.CurrentRID);
            // Start tracing
            string tracePath = Path.Combine(RunConfig.Test.TestResultFolder,
                                            $"gcperfsim-SDK{RunConfig.SDKSetting.Version}-{DotNetInfrastructure.CurrentRID}");
            CommandInvoker traceCollector = perfcollect.CollectTraceForSecs(RunConfig.SysInfo.EnvironmentVariables,
                                                                            tracePath,
                                                                            20);

            // Start app
            using (CommandInvoker gcperfsimInvoker = new(gcperfsimExecutablePath, "", env, redirectStdOutErr: false))
            {
                traceCollector.WaitForResult();
            }
        }
    }
}
