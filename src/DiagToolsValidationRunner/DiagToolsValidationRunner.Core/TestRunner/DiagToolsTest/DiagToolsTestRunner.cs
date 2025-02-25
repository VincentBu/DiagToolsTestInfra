using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public class DiagToolsTestRunner
    {
        internal readonly DiagToolsTestRunConfiguration RunConfig;
        private readonly Dictionary<string, Action> RunnerMap;
        public DiagToolsTestRunner(DiagToolsTestRunConfiguration runConfig,
                                   string baseConsoleAppSrcPath,
                                   string baseGCDumpPlaygroundSrcPath)
        {
            RunConfig = runConfig;
            RunnerMap = new()
            {
                { "dotnet-counters", this.TestDotNetCounters },
                { "dotnet-dump", this.TestDotNetDump },
                { "dotnet-gcdump", this.TestDotNetGCDump },
                { "dotnet-sos", this.TestDotNetSOS },
                { "dotnet-stack", this.TestDotNetStack },
                { "dotnet-trace", this.TestDotNetTrace }
            };

            string initLoggerPath = Path.Combine(runConfig.Test.TestResultFolder,
                                                 $"Initialization-{runConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner.Run(
                initLoggerPath, InitializeTest(runConfig, baseConsoleAppSrcPath, baseGCDumpPlaygroundSrcPath));
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
            yield return RunConfig.AppSetting.WebApp.CreateApp();

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
            yield return RunConfig.AppSetting.WebApp.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                              DotNetInfrastructure.CurrentRID);
        }

        public void TestToolsWithWebApp(string loggerPath,
                                        Func<int, IEnumerable<CommandInvokeResult>> toolTestRunner)
        {
            string webappExecutable = RunConfig.AppSetting.WebApp.GetAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                   DotNetInfrastructure.CurrentRID);

            using (CommandInvoker webappInvoker = new(webappExecutable,
                                                      "",
                                                      RunConfig.SysInfo.EnvironmentVariables,
                                                      silent: true))
            {
                try
                {
                    if (webappInvoker.Exn != null)
                    {
                        Console.WriteLine($"Fail to start webapp: {webappInvoker.Exn.Message}");
                        Console.WriteLine($"{webappInvoker.Exn.InnerException}");
                        throw webappInvoker.Exn;
                    }
                    // Wait for webapp to start
                    while (!webappInvoker.ConsoleOutput.Contains("Application started"))
                    {
                        Thread.Sleep(1000);

                        if (!String.IsNullOrEmpty(webappInvoker.ConsoleError))
                        {
                            Console.WriteLine($"Fail to run {webappInvoker.Command}:\n{webappInvoker.ConsoleError}");
                            throw new Exception();
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    // Run test
                    CommandInvokeTaskRunner.Run(loggerPath, toolTestRunner(webappInvoker.ProcessID));
                    webappInvoker.Kill(true);
                    webappInvoker.WaitForExit();
                }
            }
        }

        public void TestToolsWithGCDumpPlayground(string loggerPath,
                                                  Func<int, IEnumerable<CommandInvokeResult>> toolTestRunner)
        {
            string GCDumpPlaygroundExecutable = RunConfig.AppSetting.GCDumpPlayground.GetAppExecutable(
                RunConfig.AppSetting.BuildConfig, DotNetInfrastructure.CurrentRID);

            using (CommandInvoker GCDumpPlaygroundInvoker = new(GCDumpPlaygroundExecutable,
                                                                "0.05",
                                                                RunConfig.SysInfo.EnvironmentVariables,
                                                                silent: true))
            {
                // Run test
                try
                {
                    if (GCDumpPlaygroundInvoker.Exn != null)
                    {
                        Console.WriteLine($"Fail to start GCDumpPlayground: {GCDumpPlaygroundInvoker.Exn.Message}");
                        Console.WriteLine($"{GCDumpPlaygroundInvoker.Exn.InnerException}");
                        throw GCDumpPlaygroundInvoker.Exn;
                    }
                    // Wait for webapp to start
                    while (!GCDumpPlaygroundInvoker.ConsoleOutput.Contains("Pause for gcdumps"))
                    {
                        Thread.Sleep(1000);

                        if (!String.IsNullOrEmpty(GCDumpPlaygroundInvoker.ConsoleError))
                        {
                            Console.WriteLine($"Fail to run {GCDumpPlaygroundInvoker.Command}:\n{GCDumpPlaygroundInvoker.ConsoleError}");
                            throw new Exception();
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    CommandInvokeTaskRunner.Run(loggerPath, toolTestRunner(GCDumpPlaygroundInvoker.ProcessID));
                    GCDumpPlaygroundInvoker.Kill(true);
                    GCDumpPlaygroundInvoker.WaitForExit();
                }
            }
        }

        public void TestDiagnosticTools()
        {
            foreach (var toolName in RunConfig.ToolSetting.ToolsToTest)
            {
                Console.WriteLine($"Test tool: {toolName}");
                Action toolTestRunner = RunnerMap[toolName];
                toolTestRunner();
            }
        }
    }
}
