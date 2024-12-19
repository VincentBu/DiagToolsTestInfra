namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality

module DotNetStack =
    let TestDotNetStack (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-stack"
        
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
                $"{toolILPath} ps";
                $"{toolILPath} report -p {webappInvoker.Proc.Id}";
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