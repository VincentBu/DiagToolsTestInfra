using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetSOSTestRunner
    {
        private static IEnumerable<CommandInvokeResult> TestDotNetSOSBasicFunctionalities(
            this DiagToolsTestRunner runner)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-sos",
                                                     RunConfig.ToolSetting.Version);

            List<string> syncArgumentsList = new()
                {
                    $"{toolILPath} install",
                    $"{toolILPath} uninstall",
                    $"{toolILPath} install",
                };
            foreach (var arguments in syncArgumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables);
                yield return commandInvoker.WaitForResult();
            }
        }

        private static IEnumerable<CommandInvokeResult> TestDotNetSOSByDebuggingDump(
            this DiagToolsTestRunner runner)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;

            string dumpPath = Directory.GetFiles(RunConfig.Test.TestBed, "webapp*.dmp")
                .FirstOrDefault("");
            if (!File.Exists(dumpPath))
            {
                yield return new("",
                                 "",
                                 "",
                                 -1, 
                                 new FileNotFoundException($"{nameof(DotNetSOSTestRunner)}: Can't find dump in {RunConfig.Test.TestBed}"));
            }

            string debuggerScriptPath = Path.Combine(RunConfig.Test.TestBed, "debugging-script.txt");
            CLIDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);
            List<string> SOSDebugCommandList = new()
            {
                "clrstack",
                "clrthreads",
                "clrmodules",
                "eestack",
                "eeheap",
                "dumpstack",
                "dumpheap",
                "dso",
                "eeversion",
            };
            CLIDebugger.GenerateSOSDebuggingScript(debuggerScriptPath, SOSDebugCommandList);
            yield return debugger.DebugDump(RunConfig.SysInfo.EnvironmentVariables,
                                            DotNetInfrastructure.CurrentRID,
                                            "",
                                            dumpPath,
                                            debuggerScriptPath);
        }

        private static IEnumerable<CommandInvokeResult> TestDotNetSOSByDebuggingProcess(
            this DiagToolsTestRunner runner, int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;

            string debuggerScriptPath = Path.Combine(RunConfig.Test.TestBed, "debugging-script.txt");
            CLIDebugger debugger = new(RunConfig.SysInfo.CLIDebugger);
            List<string> SOSDebugCommandList = new()
            {
                "clrstack",
                "clrthreads",
                "clrmodules",
                "eestack",
                "eeheap",
                "dumpstack",
                "dumpheap",
                "dso",
                "eeversion",
            };
            CLIDebugger.GenerateSOSDebuggingScript(debuggerScriptPath, SOSDebugCommandList);
            yield return debugger.DebugAttachedProcess(RunConfig.SysInfo.EnvironmentVariables,
                                                        DotNetInfrastructure.CurrentRID,
                                                        "",
                                                        processID,
                                                        debuggerScriptPath);
            
        }

        public static void TestDotNetSOS(this DiagToolsTestRunner runner)
        {
            string basicLoggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                                  $"dotnet-sos.log");
            CommandInvokeTaskRunner.Run(basicLoggerPath, runner.TestDotNetSOSBasicFunctionalities());

            string dumpDebuggingLoggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                                          $"sos-dump-debugging.log");
            CommandInvokeTaskRunner.Run(dumpDebuggingLoggerPath, runner.TestDotNetSOSByDebuggingDump());

            string processDebuggingLoggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                                          $"sos-process-debugging.log");
            runner.TestToolsWithWebApp(processDebuggingLoggerPath, runner.TestDotNetSOSByDebuggingProcess);
        }
    }
}
