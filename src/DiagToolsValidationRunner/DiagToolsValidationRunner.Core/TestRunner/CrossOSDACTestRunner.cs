using DiagToolsValidationRunner.Core.Configuration.CrossOSDACTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.CrossOSDACTest
{
    public class CrossOSDACTestRunner
    {
        private readonly CrossOSDACTestRunConfiguration RunConfig;
        private readonly string ToolILPath;
        private readonly string OOMDumpPath;
        private readonly string UHEDumpPath;

        private readonly List<string> BaseSOSCommandList = new()
        {
            "clrstack",
            "clrstack -i",
            "clrthreads",
            "clrmodules",
            "eeheap",
            "dumpheap",
            "printexception",
            "dso",
            "eeversion"
        };

        public CrossOSDACTestRunner(CrossOSDACTestRunConfiguration runConfig,
                                    string baseOOMAppSrcPath,
                                    string baseUHEAppSrcPath)
        {
            RunConfig = runConfig;

            OOMDumpPath = Path.Combine(RunConfig.Test.DumpFolder, $"oom-{DotNetInfrastructure.CurrentRID}.dmp");
            UHEDumpPath = Path.Combine(RunConfig.Test.DumpFolder, $"uhe-{DotNetInfrastructure.CurrentRID}.dmp");

            // Initialize test
            string initLoggerPath = Path.Combine(runConfig.Test.TestResultFolder,
                                                 $"Initialization-{runConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner.Run(initLoggerPath,
                                        this.InitializeTest(runConfig,
                                                            baseOOMAppSrcPath,
                                                            baseUHEAppSrcPath));
            // Must set ToolILPath after initialization
            ToolILPath = DotNetTool.GetToolIL(runConfig.DotNetDumpSetting.ToolRoot,
                                              "dotnet-dump",
                                              runConfig.DotNetDumpSetting.Version);
        }

        private IEnumerable<CommandInvokeResult> InitializeTest(CrossOSDACTestRunConfiguration runConfig,
                                                                string baseOOMAppSrcPath,
                                                                string baseUHEAppSrcPath)
        {
            // Create analysis output folder and dump folder
            Directory.CreateDirectory(runConfig.Test.DumpFolder);
            Directory.CreateDirectory(runConfig.Test.AnalysisOutputFolder);

            // Generate environment activation script
            string scriptPath = Path.Combine(runConfig.Test.TestBed,
                                             $"env_activation-sdk{runConfig.SDKSetting.Version}");
            DotNetInfrastructure.GenerateEnvironmentActivationScript(DotNetInfrastructure.CurrentRID,
                                                                     scriptPath,
                                                                     runConfig.SDKSetting.DotNetRoot,
                                                                     runConfig.DotNetDumpSetting.ToolRoot);
            // Install .NET SDK
            Console.WriteLine($"Install .NET SDK {runConfig.SDKSetting.Version}");
            DotNetInfrastructure.InstallDotNetSDKByVersion(runConfig.SDKSetting.Version,
                                                           DotNetInfrastructure.CurrentRID,
                                                           runConfig.SDKSetting.DotNetRoot);

            // Install dotnet-dump
            Console.WriteLine($"Install dotnet-dump(SDK Version: {runConfig.SDKSetting.Version})");
            yield return DotNetTool.InstallDotNetTool(runConfig.SysInfo.EnvironmentVariables,
                                                      runConfig.DotNetDumpSetting.ToolRoot,
                                                      runConfig.DotNetDumpSetting.Feed,
                                                      runConfig.DotNetDumpSetting.Version,
                                                      "dotnet-dump");

            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"Prepare .NET apps for testing({RunConfig.SDKSetting.Version})");
                // 1. Create app
                yield return RunConfig.AppSetting.OOM.CreateApp();
                yield return RunConfig.AppSetting.UHE.CreateApp();

                // 2. Replace source file
                string targetOOMSrcPath = Path.Combine(RunConfig.AppSetting.OOM.AppRoot, "Program.cs");
                string OOMSrcContent = File.ReadAllText(baseOOMAppSrcPath);
                File.WriteAllText(targetOOMSrcPath, OOMSrcContent);

                string targetUHESrcPath = Path.Combine(RunConfig.AppSetting.UHE.AppRoot, "Program.cs");
                string UHESrcContent = File.ReadAllText(baseUHEAppSrcPath);
                File.WriteAllText(targetUHESrcPath, UHESrcContent);

                // 3. Build OOM and UHE
                yield return RunConfig.AppSetting.OOM.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                               DotNetInfrastructure.CurrentRID);
                yield return RunConfig.AppSetting.UHE.BuildApp(RunConfig.AppSetting.BuildConfig,
                                                               DotNetInfrastructure.CurrentRID);
            }
        }

        private IEnumerable<CommandInvokeResult> GenerateDumpsOnLinux()
        {
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);

            // Run OOM and generate dump
            DotNetInfrastructure.ActiveDotNetDumpGeneratingEnvironment(env, OOMDumpPath);
            string oomExecutablePath = RunConfig.AppSetting.OOM.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                       DotNetInfrastructure.CurrentRID);

            CommandInvoker OOMRunInvoker = new(oomExecutablePath, "", env);
            yield return OOMRunInvoker.WaitForResult();

            // Run UHE and generate dump
            DotNetInfrastructure.ActiveDotNetDumpGeneratingEnvironment(env, UHEDumpPath);
            string uheExecutablePath = RunConfig.AppSetting.UHE.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                       DotNetInfrastructure.CurrentRID);

            CommandInvoker UHERunInvoker = new(uheExecutablePath, "", env);
            yield return UHERunInvoker.WaitForResult();
        }

        public void TestDACOnLinux()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            // Generate dumps
            string initLoggerPath = Path.Combine(RunConfig.Test.TestResultFolder,
                                                 $"Initialization-{RunConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner.Run(initLoggerPath,
                                        this.GenerateDumpsOnLinux(),
                                        true);

            // Analyze dumps
            DotNetDumpAnalyzer dumpAnalyzer = new(ToolILPath);

            CommandInvokeResult OOMDumpAnalyzeResult = dumpAnalyzer.DebugDump(RunConfig.SysInfo.EnvironmentVariables,
                                                                              "",
                                                                              OOMDumpPath,
                                                                              BaseSOSCommandList);
            string OOMDumpName = Path.GetFileNameWithoutExtension(OOMDumpPath);
            string OOMDumpAnalysisOutputPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, $"{OOMDumpName}.log");
            CommandInvokeTaskRunner.RecordSingle(OOMDumpAnalysisOutputPath, OOMDumpAnalyzeResult);

            CommandInvokeResult UHEDumpAnalyzeResult = dumpAnalyzer.DebugDump(RunConfig.SysInfo.EnvironmentVariables,
                                                                              "",
                                                                              UHEDumpPath,
                                                                              BaseSOSCommandList);
            string UHEDumpName = Path.GetFileNameWithoutExtension(UHEDumpPath);
            string UHEDumpAnalysisOutputPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, $"{UHEDumpName}.log");
            CommandInvokeTaskRunner.RecordSingle(UHEDumpAnalysisOutputPath, UHEDumpAnalyzeResult);
        }

        public void TestDACOnWindows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            List<string> dumpList = new();
            if (RunConfig.Test.TestName.EndsWith("-x64"))
            {
                dumpList = Directory.GetFiles(RunConfig.Test.DumpFolder, "*-x64.dmp")
                    .Concat(Directory.GetFiles(RunConfig.Test.DumpFolder, "*-arm64.dmp"))
                    .ToList();
            }
            else if (RunConfig.Test.TestName.EndsWith("-x86"))
            {
                dumpList = Directory.GetFiles(RunConfig.Test.DumpFolder, "*-arm.dmp")
                    .ToList();
            }
            else
            {
                throw new Exception($"{nameof(CrossOSDACTestRunner)}: Only support win-x64 or win-x86 SDK.");
            }

            DotNetDumpAnalyzer dumpAnalyzer = new(ToolILPath);
            foreach (var dumpPath in dumpList)
            {
                string dumpName = Path.GetFileNameWithoutExtension(dumpPath);
                string symbolFolder = string.Empty;
                string rid = String.Join("-", dumpName.Split("-").ToList().Slice(1, 2));
                if (dumpName.StartsWith("oom"))
                {
                    symbolFolder = RunConfig.AppSetting.OOM.GetSymbolFolder(RunConfig.AppSetting.BuildConfig, rid);
                }
                else if (dumpName.StartsWith("uhe"))
                {
                    symbolFolder = RunConfig.AppSetting.UHE.GetSymbolFolder(RunConfig.AppSetting.BuildConfig, rid);
                }
                else
                {
                    Console.WriteLine($"{nameof(CrossOSDACTestRunner)}: Ignore unknown dump {dumpPath}.");
                    continue;
                }

                List<string> debugCommandList = new(BaseSOSCommandList);
                debugCommandList.Insert(0, $"setsymbolserver -directory {symbolFolder}");

                CommandInvokeResult dumpAnalyzeResult = dumpAnalyzer.DebugDump(RunConfig.SysInfo.EnvironmentVariables,
                                                                               "",
                                                                               dumpPath,
                                                                               debugCommandList);

                string dumpAnalysisOutputPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, $"{dumpName}-win.log");
                CommandInvokeTaskRunner.RecordSingle(dumpAnalysisOutputPath, dumpAnalyzeResult);
            }
        }
    }
}
