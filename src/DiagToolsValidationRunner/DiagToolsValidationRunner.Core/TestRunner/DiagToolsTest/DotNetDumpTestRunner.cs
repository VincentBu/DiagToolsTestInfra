using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetDumpTestRunner
    {
        private static IEnumerable<CommandInvokeResult> TestDotNetDumpWithWebApp(this DiagToolsTestRunner runner,
                                                                                 int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-dump",
                                                     RunConfig.ToolSetting.Version);

            string dumpPath = Path.Combine(RunConfig.Test.TestBed, $"webapp-{processID}.dmp");
            List<string> syncArgumentsList = new()
            {
                $"{toolILPath} --help",
                $"{toolILPath} ps",
                $"{toolILPath} collect -p {processID} -o {dumpPath}",
            };
            foreach (var arguments in syncArgumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables);
                yield return commandInvoker.WaitForResult();
            }

            List<string> SOSCommandList = new()
            {
                "clrstack",
                "clrthreads",
                "clrmodules",
                "eeheap",
                "dumpheap",
                "dso",
                "eeversion"
            };
            DotNetDumpAnalyzer dumpAnalyzer = new(toolILPath);
            yield return dumpAnalyzer.DebugDump(RunConfig.SysInfo.EnvironmentVariables,
                                                "",
                                                dumpPath,
                                                SOSCommandList);
        }

        public static void TestDotNetDump(this DiagToolsTestRunner runner)
        {
            string loggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                             $"dotnet-dump.log");
            runner.TestToolsWithWebApp(loggerPath, runner.TestDotNetDumpWithWebApp);
        }
    }
}
