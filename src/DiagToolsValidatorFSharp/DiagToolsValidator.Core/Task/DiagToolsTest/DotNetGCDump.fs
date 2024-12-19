namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality

module DotNetGCDump =
    let TestDotNetGCDump (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-gcdump"
        
        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- configuration.DotNet.DotNetRoot
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)

        trace {
            let! toolILPath = DotNetTool.GetToolIL configuration.DiagTool.ToolRoot toolName configuration.DiagTool.DiagToolVersion

            // Test with GCDumpPlayground
            let gcDumpPlaygroundInvokeResult = TestInfrastructure.RunGCDumpPlayground(configuration)
            yield! gcDumpPlaygroundInvokeResult

            let! gcDumpPlaygroundInvoker = gcDumpPlaygroundInvokeResult

            for arguments in [
                $"{toolILPath} --help";
                $"{toolILPath} ps";
                $"{toolILPath} collect -p {gcDumpPlaygroundInvoker.Proc.Id} -v";
            ] do
                yield! DotNet.RunDotNetCommand configuration.DotNet.DotNetRoot
                                               arguments
                                               configuration.TestResultFolder
                                               true
                                               CommandLineTool.PrinteOutputData
                                               CommandLineTool.PrintErrorData
                                               true

            CommandLineTool.TerminateCommandInvoker(gcDumpPlaygroundInvoker) |> ignore
        }

