using System.Xml.Linq;

using DiagToolsValidationRunner.Core.Configuration.DebuggerExtensionTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DebuggerExtensionTest
{
    public class DebuggerExtensionTestRunner
    {
        public static void GenerateNugetConfig(string feed, string userName, string token, string nugetPath)
        {
            string configContent =
$"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="dotnet10" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json" />
    <add key="dotnet9" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json" />
    <add key="dotnet8" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json" />
    <add key="internalPackageSource" value="{feed}" />
  </packageSources>
  <packageSourceCredentials>
    <internalPackageSource>
        <add key="Username" value="{userName}" /> 
        <add key="CleartextPassword" value="{token}" />
    </internalPackageSource>
  </packageSourceCredentials>
</configuration>
""";
            var nugetConfig = XElement.Parse(configContent);
            nugetConfig.Save(nugetPath);
        }

        private readonly List<string> SOSDebugCommandList = new()
        {
            "clrthreads",
            "verifyheap",
            "dumpheap -stat",
            "dumpasync",
            "dumplog",
            "crashinfo",
        };

        private readonly DebuggerExtensionTestRunConfiguration RunConfig;

        public DebuggerExtensionTestRunner(DebuggerExtensionTestRunConfiguration runConfig,
                                           string baseNativeAOTAppSrcPath)
        {
            RunConfig = runConfig;
            string initLoggerPath = Path.Combine(runConfig.Test.TestResultFolder,
                                                    $"Initialization-{runConfig.SDKSetting.Version}.log");
            CommandInvokeTaskRunner runner = new(initLoggerPath);
            runner.Run(this.InitializeTest(runConfig, baseNativeAOTAppSrcPath));
        }

        private IEnumerable<CommandInvokeResult> InitializeTest(DebuggerExtensionTestRunConfiguration runConfig,
                                                                string baseNativeAOTAppSrcPath)
        {
            // Create analysis output folder and dump folder
            Directory.CreateDirectory(runConfig.Test.AnalysisOutputFolder);
            Directory.CreateDirectory(runConfig.Test.DumpFolder);


            // Generate environment activation script
            string scriptPath = Path.Combine();
            DotNetInfrastructure.GenerateEnvironmentActivationScript(DotNetInfrastructure.CurrentRID,
                                                                     scriptPath,
                                                                     runConfig.SDKSetting.DotNetRoot,
                                                                     runConfig.DebuggerExtensionSetting.ToolRoot);
            // Install .NET SDK
            Console.WriteLine($"Install .NET SDK {runConfig.SDKSetting.Version}");
            DotNetInfrastructure.InstallDotNetSDKByVersion(runConfig.SDKSetting.Version,
                                                           DotNetInfrastructure.CurrentRID,
                                                           runConfig.SDKSetting.DotNetRoot);

            // Prepare app for testing (create, modify source code and project file, publish)
            Console.WriteLine($"Prepare .NET apps for testing({runConfig.SDKSetting.Version})");

            // 1. Create app
            yield return runConfig.AppSetting.NativeAOTApp.CreateApp();

            // 2. Generate Nuget.config
            string nuGetPath = Path.Combine(runConfig.AppSetting.NativeAOTApp.AppRoot, "NuGet.config");
            DebuggerExtensionTestRunner.GenerateNugetConfig(runConfig.DebuggerExtensionSetting.Feed,
                                                            runConfig.DebuggerExtensionSetting.UserName,
                                                            runConfig.DebuggerExtensionSetting.Token,
                                                            nuGetPath);

            // 3. Modify project file
            string projectFile = runConfig.AppSetting.NativeAOTApp.GetProjectFilePath();
            string xmlData = File.ReadAllText(projectFile);
            XDocument doc = XDocument.Parse(xmlData);
            XElement? propertyGroup = doc?.Root?.Element("PropertyGroup");
            propertyGroup?.Add(new XElement("AllowUnsafeBlocks", "true"));
            propertyGroup?.Add(new XElement("PublishAot", "true"));
            doc?.Save(projectFile);

            // 4. Replace source file
            string targetNativeAOTAppSrcPath = Path.Combine(runConfig.AppSetting.NativeAOTApp.AppRoot, "Program.cs");
            string consoleSrcContent = File.ReadAllText(baseNativeAOTAppSrcPath);
            File.WriteAllText(targetNativeAOTAppSrcPath, consoleSrcContent);

            // 5. Publish nativeaot
            yield return runConfig.AppSetting.NativeAOTApp.PublishApp(runConfig.AppSetting.BuildConfig,
                                                                      DotNetInfrastructure.CurrentRID);

            // Install dotnet-debugger-extensions tool
            Console.WriteLine($"Install dotnet-debugger-extensions(SDK Version: {runConfig.SDKSetting.Version})");
            yield return DotNetTool.InstallDotNetTool(runConfig.SysInfo.EnvironmentVariables,
                                                      runConfig.DebuggerExtensionSetting.ToolRoot,
                                                      runConfig.DebuggerExtensionSetting.Feed,
                                                      runConfig.DebuggerExtensionSetting.Version,
                                                      "dotnet-debugger-extensions",
                                                      nuGetPath);

            // Install extension
            string toolName = "dotnet-debugger-extensions";
            string toolILPath = DotNetTool.GetToolIL(RunConfig.DebuggerExtensionSetting.ToolRoot,
                                                     toolName,
                                                     RunConfig.DebuggerExtensionSetting.Version);
            // Install extension
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(RunConfig.SysInfo.EnvironmentVariables);
            CommandInvoker extensionInstallInvoker = new(dotNetExecutable,
                                                         $"{toolILPath} install --accept-license-agreement",
                                                         RunConfig.SysInfo.EnvironmentVariables);
            yield return extensionInstallInvoker.InvokeCommand(true);
        }

        private IEnumerable<CommandInvokeResult> TestByDebuggingDump()
        {
            // Active dump and stresslog generated environment
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);
            if (DotNetInfrastructure.CurrentRID.Contains("win"))
            {
                DotNetInfrastructure.ActiveWin32DumpGeneratingEnvironment(RunConfig.Test.DumpFolder);
            }
            else
            {
                string linuxDumpPath = Path.Combine(RunConfig.Test.DumpFolder, $"dump-debugging.dmp");
                DotNetInfrastructure.ActiveDotNetDumpGeneratingEnvironment(env, linuxDumpPath);
            }

            string stressLogPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, $"StressLog-dump-debugging.log");
            DotNetInfrastructure.ActiveStressLogEnvironment(env, stressLogPath);

            // Run nativeaot

            string nativeaotExecutablePath = RunConfig.AppSetting.NativeAOTApp.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                                      DotNetInfrastructure.CurrentRID);

            CommandInvoker nativeaotRunInvoker = new(nativeaotExecutablePath, "", env);
            yield return nativeaotRunInvoker.InvokeCommand(true);
            string dumpPath = DotNetInfrastructure.CurrentRID.Contains("win") switch
            {
                true => Directory.GetFiles(RunConfig.Test.DumpFolder, "*.dmp").FirstOrDefault(""),
                false => Path.Combine(RunConfig.Test.DumpFolder, $"dump-debugging.dmp")
            };

            // Generate debug script
            string debugDumpScriptPath = Path.Combine(RunConfig.Test.TestResultFolder, "dump-debug-script.txt");
            SOSDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);
            debugger.GenerateDebugScript(DotNetInfrastructure.CurrentRID, debugDumpScriptPath, SOSDebugCommandList);
            yield return debugger.DebugDump(env, DotNetInfrastructure.CurrentRID, "", dumpPath, debugDumpScriptPath);
        }

        private IEnumerable<CommandInvokeResult> TestByDebuggingProcess()
        {
            // Active stresslog generated environment
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);

            string stressLogPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, $"StressLog-process-debugging.log");
            DotNetInfrastructure.ActiveStressLogEnvironment(env, stressLogPath);

            // Startup nativeaot with debugger
            string nativeaotExecutablePath = RunConfig.AppSetting.NativeAOTApp.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                                      DotNetInfrastructure.CurrentRID);
            string debugProcessScriptPath = Path.Combine(RunConfig.Test.TestResultFolder, "process-debug-script.txt");
            SOSDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);
            debugger.GenerateDebugScript(DotNetInfrastructure.CurrentRID, debugProcessScriptPath, SOSDebugCommandList);
            yield return debugger.DebugLaunchable(env,
                                                  DotNetInfrastructure.CurrentRID,
                                                  "",
                                                  nativeaotExecutablePath,
                                                  debugProcessScriptPath);
        }

        public void TestDebuggerExtension()
        {
            string testDumpDebuggingLogPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, "debug-dump.log");
            // Ignore error since running nativeaot can raise exception which is expected.
            CommandInvokeTaskRunner dumpDebuggingTaskRunner = new(testDumpDebuggingLogPath, true);
            dumpDebuggingTaskRunner.Run(TestByDebuggingDump());

            string testProcessDebuggingLogPath = Path.Combine(RunConfig.Test.AnalysisOutputFolder, "debug-process.log");
            // Ignore error since running nativeaot can raise exception which is expected.
            CommandInvokeTaskRunner processDebuggingTaskRunner = new(testProcessDebuggingLogPath);
            dumpDebuggingTaskRunner.Run(TestByDebuggingProcess());
        }
    }
}
