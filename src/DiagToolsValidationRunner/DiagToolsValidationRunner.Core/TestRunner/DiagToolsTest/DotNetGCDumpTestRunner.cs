using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetGCDumpTestRunner
    {
        private static IEnumerable<CommandInvokeResult> 
            TestDotNetGCDumpWithGCDumpPlayground(this DiagToolsTestRunner runner, int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-gcdump",
                                                     RunConfig.ToolSetting.Version);

            List<string> argumentsList = new()
            {
                $"{toolILPath} --help",
                $"{toolILPath} ps",
                $"{toolILPath} collect -p {processID} -v"
            };

            foreach (var arguments in argumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables,
                                                    workDirectory: RunConfig.Test.TestResultFolder);
                yield return commandInvoker.WaitForResult();
            }
            
        }

        public static void TestDotNetGCDump(this DiagToolsTestRunner runner)
        {
            string loggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                             $"dotnet-gcdump.log");
            runner.TestToolsWithGCDumpPlayground(loggerPath, runner.TestDotNetGCDumpWithGCDumpPlayground);
        }
    }
}
