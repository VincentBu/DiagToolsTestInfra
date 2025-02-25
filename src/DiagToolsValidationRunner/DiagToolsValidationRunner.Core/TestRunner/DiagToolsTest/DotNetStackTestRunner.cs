using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;
using System.Diagnostics;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetStackTestRunner
    {
        private static IEnumerable<CommandInvokeResult> TestDotNetStackWithWebApp(this DiagToolsTestRunner runner,
                                                                                  int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-stack",
                                                     RunConfig.ToolSetting.Version);

            List<string> argumentsList = new()
            {
                $"{toolILPath} --help",
                $"{toolILPath} ps",
                $"{toolILPath} report -p {processID}"
            };
            foreach (var arguments in argumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables);
                yield return commandInvoker.WaitForResult();
            }
            
        }

        public static void TestDotNetStack(this DiagToolsTestRunner runner)
        {
            string loggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                             $"dotnet-stack.log");
            runner.TestToolsWithWebApp(loggerPath, runner.TestDotNetStackWithWebApp);
        }
    }
}
