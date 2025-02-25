using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetTraceTestRunner
    {
        private static IEnumerable<CommandInvokeResult> TestDotNetTraceWithConsoleApp(this DiagToolsTestRunner runner)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-trace",
                                                     RunConfig.ToolSetting.Version);

            string consoleAppExecutable = RunConfig.AppSetting.ConsoleApp.GetAppExecutable(
                RunConfig.AppSetting.BuildConfig, DotNetInfrastructure.CurrentRID);

            string netTracePath = Path.Combine(RunConfig.Test.TestResultFolder, "consoleapp.nettrace");
            string arguments = $"{toolILPath} collect -o {netTracePath} --providers Microsoft-Windows-DotNETRuntime -- {consoleAppExecutable}";
            CommandInvoker consoleAppTracingInvoker = new(dotNetExecutable,
                                                          arguments,
                                                          RunConfig.SysInfo.EnvironmentVariables);

            yield return consoleAppTracingInvoker.WaitForResult();
        }

        private static IEnumerable<CommandInvokeResult> TestDotNetTraceWithWebApp(this DiagToolsTestRunner runner,
                                                                                  int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-trace",
                                                     RunConfig.ToolSetting.Version);

            string netTracePath = Path.Combine(RunConfig.Test.TestResultFolder, "webapp.nettrace");
            string speedScopePath = Path.Combine(RunConfig.Test.TestResultFolder, "webapp.speedscope");
            List<string> argumentsList = new()
            {
                $"{toolILPath} --help",
                $"{toolILPath} list-profiles",
                $"{toolILPath} ps",
                $"{toolILPath} collect -p {processID} -o {netTracePath} --duration 00:00:10",
                $"{toolILPath} convert {netTracePath} --format speedscope -o {speedScopePath}",
            };
            foreach (var arguments in argumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables);
                yield return commandInvoker.WaitForResult();
            }
            
        }

        public static void TestDotNetTrace(this DiagToolsTestRunner runner)
        {
            string loggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                             $"dotnet-trace.log");
            runner.TestToolsWithWebApp(loggerPath, runner.TestDotNetTraceWithWebApp);
            CommandInvokeTaskRunner.Run(loggerPath, runner.TestDotNetTraceWithConsoleApp());
        }
    }
}
