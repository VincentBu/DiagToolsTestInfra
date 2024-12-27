namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Threading
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.Functionality


module DotNetCounters =
    let TestDotNetCounters (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-counters"
        
        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- configuration.DotNet.DotNetRoot
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)
        trace {
            let! toolILPath = DotNetTool.GetToolIL configuration.DiagTool.ToolRoot toolName configuration.DiagTool.DiagToolVersion

            // Test with webapp
            let webappInvokerResult = TestInfrastructure.RunWebapp(configuration)
            yield! webappInvokerResult

            let! webappInvoker = webappInvokerResult

            for arguments in [
                $"{toolILPath} --help";
                $"{toolILPath} list";
                $"{toolILPath} ps";
            ] do
                yield! DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                               arguments
                                               configuration.TestResultFolder
                                               true
                                               CommandLineTool.PrinteOutputData
                                               CommandLineTool.PrintErrorData
                                               true

            for arguments in [
                $"{toolILPath} collect  -o webapp_counter.csv -p {webappInvoker.Proc.Id}";
                $"{toolILPath} monitor -p {webappInvoker.Proc.Id}";
            ] do
                let invokerResult = DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                                            arguments
                                                            configuration.TestResultFolder
                                                            false
                                                            CommandLineTool.IgnoreOutputData
                                                            CommandLineTool.IgnoreErrorData
                                                            false
                Thread.Sleep(10000)
                let! invoker = invokerResult
                let terminateResult = CommandLineTool.TerminateCommandInvoker(invoker)
                Thread.Sleep(5000)
                yield! terminateResult

            CommandLineTool.TerminateCommandInvoker(webappInvoker) |> ignore

            // Test with console
            let! consoleAppExecutable = configuration.TargetApp.ConsoleApp.GetAppExecutable(configuration.TargetApp.BuildConfig)
            for arguments in [
                $"{toolILPath} collect -o console_counter.csv -- {consoleAppExecutable}";
                $"{toolILPath} monitor -- {consoleAppExecutable}";
            ] do
                yield! DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                               arguments
                                               configuration.TestResultFolder
                                               false
                                               CommandLineTool.IgnoreOutputData
                                               CommandLineTool.IgnoreErrorData
                                               true

        }
        