namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality

module DotNetTrace =
    let TestDotNetTrace (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-trace"
        
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
            let netTracePath = Path.Combine(configuration.TestResultFolder, "webapp.nettrace")
            let speedScopePath = Path.Combine(configuration.TestResultFolder, "webapp.speedscope")

            for arguments in [
                $"{toolILPath} --help";
                $"{toolILPath} list-profiles";
                $"{toolILPath} ps";
                $"{toolILPath} collect -p {webappInvoker.Proc.Id} -o {netTracePath} --duration 00:00:10";
                $"{toolILPath} convert {netTracePath} --format speedscope -o {speedScopePath}";
            ] do
                yield! DotNet.RunDotNetCommand configuration.DotNet.DotNetRoot
                                               arguments
                                               configuration.TestResultFolder
                                               true
                                               CommandLineTool.PrinteOutputData
                                               CommandLineTool.PrintErrorData
                                               true
            
            CommandLineTool.TerminateCommandInvoker(webappInvoker) |> ignore
        }
