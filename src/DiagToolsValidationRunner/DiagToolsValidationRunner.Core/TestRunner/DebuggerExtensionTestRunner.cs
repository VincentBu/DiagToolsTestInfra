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
            CommandInvokeTaskRunner.Run(initLoggerPath, this.InitializeTest(runConfig, baseNativeAOTAppSrcPath));
        }

        private IEnumerable<CommandInvokeResult> InitializeTest(DebuggerExtensionTestRunConfiguration runConfig,
                                                                string baseNativeAOTAppSrcPath)
        {
            // Create analysis output folder and dump folder
            Directory.CreateDirectory(runConfig.Test.DumpDebuggingOutputFolder);
            Directory.CreateDirectory(runConfig.Test.LiveSessionDebuggingOutputFolder);

            // Generate environment activation script
            string scriptPath = Path.Combine(runConfig.Test.TestBed,
                                             $"env_activation-sdk{runConfig.SDKSetting.Version}");
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
                                                      configFilePath: nuGetPath);

            // Run installer
            string toolName = "dotnet-debugger-extensions";
            string toolILPath = DotNetTool.GetToolIL(RunConfig.DebuggerExtensionSetting.ToolRoot,
                                                     toolName,
                                                     RunConfig.DebuggerExtensionSetting.Version);

            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(RunConfig.SysInfo.EnvironmentVariables);
            CommandInvoker extensionInstallInvoker = new(dotNetExecutable,
                                                         $"{toolILPath} install --accept-license-agreement",
                                                         RunConfig.SysInfo.EnvironmentVariables);
            yield return extensionInstallInvoker.WaitForResult();
        }

        private IEnumerable<CommandInvokeResult> TestByDebuggingDump()
        {
            Directory.CreateDirectory(RunConfig.Test.DumpDebuggingOutputFolder);

            // Active dump and stresslog generated environment
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);
            if (DotNetInfrastructure.CurrentRID.Contains("win"))
            {
                DotNetInfrastructure.ActiveWin32DumpGeneratingEnvironment(RunConfig.Test.DumpDebuggingOutputFolder);
            }
            else
            {
                string linuxDumpPath = Path.Combine(RunConfig.Test.DumpDebuggingOutputFolder, $"nativeaot-dump.dmp");
                DotNetInfrastructure.ActiveDotNetDumpGeneratingEnvironment(env, linuxDumpPath);
            }

            DotNetInfrastructure.ActiveStressLogEnvironment(env);

            // Copy createdump to native folder(non-windows platform)
            if (!OperatingSystem.IsWindows())
            {
                string symbolFolder = RunConfig.AppSetting.NativeAOTApp.GetSymbolFolder(RunConfig.AppSetting.BuildConfig,
                                                                                        DotNetInfrastructure.CurrentRID);
                string createDumpSrcPath = Path.Combine(symbolFolder, "createdump");
                string nativeSymbolFolder = RunConfig.AppSetting.NativeAOTApp.GetNativeSymbolFolder(RunConfig.AppSetting.BuildConfig,
                                                                                                    DotNetInfrastructure.CurrentRID);
                string createDumpDstPath = Path.Combine(nativeSymbolFolder, "createdump");
                File.Copy(createDumpSrcPath, createDumpDstPath);
            }

            // Run nativeaot
            string nativeaotExecutablePath = RunConfig.AppSetting.NativeAOTApp.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                                      DotNetInfrastructure.CurrentRID);

            CommandInvoker nativeaotRunInvoker = new(nativeaotExecutablePath, "", env);
            yield return nativeaotRunInvoker.WaitForResult();
            string dumpPath = DotNetInfrastructure.CurrentRID.Contains("win") switch
            {
                true => Directory.GetFiles(RunConfig.Test.DumpDebuggingOutputFolder, "*.dmp").FirstOrDefault(""),
                false => Path.Combine(RunConfig.Test.DumpDebuggingOutputFolder, $"nativeaot-dump.dmp")
            };

            CLIDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);

            // Generate debug script
            string debugDumpScriptPath = Path.Combine(RunConfig.Test.DumpDebuggingOutputFolder, "debug-dump-script.txt");
            CLIDebugger.GenerateSOSDebuggingScript(debugDumpScriptPath, SOSDebugCommandList);
            yield return debugger.DebugDump(env,
                                            DotNetInfrastructure.CurrentRID,
                                            RunConfig.Test.DumpDebuggingOutputFolder,
                                            dumpPath,
                                            debugDumpScriptPath);
        }

        private IEnumerable<CommandInvokeResult> TestByDebuggingProcess()
        {
            Directory.CreateDirectory(RunConfig.Test.LiveSessionDebuggingOutputFolder);

            // Active stresslog generated environment
            Dictionary<string, string> env = new(RunConfig.SysInfo.EnvironmentVariables);

            if (DotNetInfrastructure.CurrentRID.Contains("win"))
            {
                DotNetInfrastructure.ActiveWin32DumpGeneratingEnvironment(RunConfig.Test.LiveSessionDebuggingOutputFolder);
            }

            DotNetInfrastructure.ActiveStressLogEnvironment(env);

            // Startup nativeaot with debugger
            string nativeaotExecutablePath = RunConfig.AppSetting.NativeAOTApp.GetNativeAppExecutable(RunConfig.AppSetting.BuildConfig,
                                                                                                      DotNetInfrastructure.CurrentRID);

            CLIDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);
            // Generate debug script
            string debugProcessScriptPath = Path.Combine(RunConfig.Test.LiveSessionDebuggingOutputFolder, "debug-process-script.txt");
            List<string> debugCommandList = new(SOSDebugCommandList);
            if (!OperatingSystem.IsWindows())
            {
                debugCommandList.Insert(0, "run");
            }
            CLIDebugger.GenerateSOSDebuggingScript(debugProcessScriptPath, debugCommandList);
            yield return debugger.DebugLaunchable(env,
                                                  DotNetInfrastructure.CurrentRID,
                                                  RunConfig.Test.LiveSessionDebuggingOutputFolder,
                                                  nativeaotExecutablePath,
                                                  debugProcessScriptPath);
        }

        public void TestDebuggerExtension()
        {
            string testDumpDebuggingLogPath = Path.Combine(RunConfig.Test.DumpDebuggingOutputFolder,
                                                           "debug-dump.log");
            // Ignore error since running nativeaot can raise exception which is expected.
            CommandInvokeTaskRunner.Run(testDumpDebuggingLogPath, TestByDebuggingDump(), true);

            string testProcessDebuggingLogPath = Path.Combine(RunConfig.Test.LiveSessionDebuggingOutputFolder,
                                                              "debug-process.log");
            // Ignore error since running nativeaot can raise exception which is expected.
            CommandInvokeTaskRunner.Run(testProcessDebuggingLogPath, TestByDebuggingProcess(), true);
        }
    }
}
