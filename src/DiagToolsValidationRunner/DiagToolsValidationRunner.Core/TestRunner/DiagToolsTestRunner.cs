using DiagToolsValidationRunner.Core.Configuration.DebuggerExtensionTest;
using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagToolsValidationRunner.Core.TestRunner
{
    public class DiagToolsTestRunner
    {
        private readonly DiagToolsTestRunConfiguration RunConfig;

        public DiagToolsTestRunner(DiagToolsTestRunConfiguration runConfig,
                                   string baseConsoleAppSrcPath,
                                   string baseGCDumpPlaygroundSrcPath)
        {
            RunConfig = runConfig;
            string initLoggerPath = Path.Combine(runConfig.Test.TestResultFolder,
                                                    $"Initialization-{runConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner.Run(
                initLoggerPath, this.InitializeTest(runConfig, baseConsoleAppSrcPath, baseGCDumpPlaygroundSrcPath));
        }

        private IEnumerable<CommandInvokeResult> InitializeTest(DiagToolsTestRunConfiguration runConfig,
                                                                string baseConsoleAppSrcPath,
                                                                string baseGCDumpPlaygroundSrcPath)
        {
            // Create testresult folder
            Directory.CreateDirectory(runConfig.Test.TestResultFolder);

            // Generate environment activation script
            string scriptPath = Path.Combine(runConfig.Test.TestBed,
                                             $"env_activation-sdk{runConfig.SDKSetting.Version}");
            DotNetInfrastructure.GenerateEnvironmentActivationScript(DotNetInfrastructure.CurrentRID,
                                                                     scriptPath,
                                                                     runConfig.SDKSetting.DotNetRoot,
                                                                     runConfig.ToolSetting.ToolRoot);

            // Install .NET SDK
            Console.WriteLine($"Install .NET SDK {runConfig.SDKSetting.Version}");
            DotNetInfrastructure.InstallDotNetSDKByVersion(runConfig.SDKSetting.Version,
                                                           DotNetInfrastructure.CurrentRID,
                                                           runConfig.SDKSetting.DotNetRoot);

            // Install diag tools
            foreach (var toolName in runConfig.ToolSetting.ToolsToTest)
            {
                Console.WriteLine($"Install {toolName})");
                yield return DotNetTool.InstallDotNetTool(runConfig.SysInfo.EnvironmentVariables,
                                                          runConfig.ToolSetting.ToolRoot,
                                                          runConfig.ToolSetting.Feed,
                                                          runConfig.ToolSetting.Version,
                                                          toolName);
            }

            Console.WriteLine("Prepare .NET apps for testing");
            // 1. Create app
            yield return RunConfig.AppSetting.GCDumpPlayground.CreateApp();
            yield return RunConfig.AppSetting.ConsoleApp.CreateApp();

            // 2. Replace source file
            string targetGCDumpPlaygroundSrcPath = Path.Combine(
                RunConfig.AppSetting.GCDumpPlayground.AppRoot, "Program.cs");
            string GCDumpPlaygroundSrcContent = File.ReadAllText(baseGCDumpPlaygroundSrcPath);
            File.WriteAllText(targetGCDumpPlaygroundSrcPath, GCDumpPlaygroundSrcContent);

            string targetConsoleAppSrcPath = Path.Combine(RunConfig.AppSetting.ConsoleApp.AppRoot, "Program.cs");
            string ConsoleAppSrcContent = File.ReadAllText(baseConsoleAppSrcPath);
            File.WriteAllText(targetConsoleAppSrcPath, ConsoleAppSrcContent);

            // 3. Build Console and GCDumpPlayground
            yield return RunConfig.AppSetting.GCDumpPlayground.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                                        DotNetInfrastructure.CurrentRID);
            yield return RunConfig.AppSetting.ConsoleApp.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                                  DotNetInfrastructure.CurrentRID);
        }
    }
}
