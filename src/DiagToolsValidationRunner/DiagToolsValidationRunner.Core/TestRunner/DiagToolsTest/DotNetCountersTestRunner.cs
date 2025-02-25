using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.TestRunner.DiagToolsTest
{
    public static class DotNetCountersTestRunner
    {
        private static IEnumerable<CommandInvokeResult> TestDotNetCountersWithConsoleApp(this DiagToolsTestRunner runner)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-counters",
                                                     RunConfig.ToolSetting.Version);

            string consoleAppExecutable = RunConfig.AppSetting.ConsoleApp.GetAppExecutable(
                RunConfig.AppSetting.BuildConfig, DotNetInfrastructure.CurrentRID);

            string consoleAppCounterCsvPath = Path.Combine(RunConfig.Test.TestResultFolder, "consoleapp_counter.csv");
            string arguments = $"{toolILPath} collect -o {consoleAppCounterCsvPath} -- {consoleAppExecutable}";
            CommandInvoker consoleAppTracingInvoker = new(dotNetExecutable,
                                                          arguments,
                                                          RunConfig.SysInfo.EnvironmentVariables);

            yield return consoleAppTracingInvoker.WaitForResult();
        }

        private static IEnumerable<CommandInvokeResult> TestDotNetCountersWithWebApp(this DiagToolsTestRunner runner,
                                                                                     int processID)
        {
            DiagToolsTestRunConfiguration RunConfig = runner.RunConfig;
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(
                RunConfig.SysInfo.EnvironmentVariables);
            string toolILPath = DotNetTool.GetToolIL(RunConfig.ToolSetting.ToolRoot,
                                                     "dotnet-counters",
                                                     RunConfig.ToolSetting.Version);

            List<string> syncArgumentsList = new()
            {
                $"{toolILPath} --help",
                $"{toolILPath} ps"
            };
            foreach (var arguments in syncArgumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables);
                yield return commandInvoker.WaitForResult();
            }

            string webAppCounterCsvPath = Path.Combine(RunConfig.Test.TestResultFolder, "webapp_counter.csv");
            List<string> asyncArgumentsList = new()
            {
                $"{toolILPath} collect -o {webAppCounterCsvPath} -p {processID}",
                $"{toolILPath} monitor -p {processID}"
            };
            foreach (var arguments in asyncArgumentsList)
            {
                CommandInvoker commandInvoker = new(dotNetExecutable,
                                                    arguments,
                                                    RunConfig.SysInfo.EnvironmentVariables,
                                                    redirectStdOutErr: false);
                    
                Thread.Sleep(5000);
                yield return commandInvoker.TerminateForResult();
            }
            
        }

        public static void TestDotNetCounters(this DiagToolsTestRunner runner)
        {
            string loggerPath = Path.Combine(runner.RunConfig.Test.TestResultFolder,
                                             $"dotnet-counters.log");
            runner.TestToolsWithWebApp(loggerPath, runner.TestDotNetCountersWithWebApp);
            CommandInvokeTaskRunner.Run(loggerPath, runner.TestDotNetCountersWithConsoleApp());
        }
    }
}
