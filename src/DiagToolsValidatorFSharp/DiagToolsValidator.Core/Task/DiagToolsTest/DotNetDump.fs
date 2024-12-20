namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality

module DotNetDump =
    let TestDotNetDump (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-dump"

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
            
            let dumpPath = Path.Combine(configuration.TestBed, $"webapp-{webappInvoker.Proc.Id}.dmp")
            for arguments in [
                $"{toolILPath} --help";
                $"{toolILPath} ps";
                $"{toolILPath} collect -p {webappInvoker.Proc.Id} -o {dumpPath}";
            ] do
                yield! DotNet.RunDotNetCommand configuration.DotNet.DotNetRoot
                                               arguments
                                               configuration.TestResultFolder
                                               true
                                               CommandLineTool.PrinteOutputData
                                               CommandLineTool.PrintErrorData
                                               true
                
            CommandLineTool.TerminateCommandInvoker(webappInvoker) |> ignore

            // Analyze dump
            let analyzeInvokeResult = DotNet.RunDotNetCommand configuration.DotNet.DotNetRoot
                                                              $"{toolILPath} analyze {dumpPath}"
                                                              configuration.TestResultFolder
                                                              true
                                                              CommandLineTool.IgnoreOutputData
                                                              CommandLineTool.IgnoreErrorData
                                                              false
            let! analyzeInvoke = analyzeInvokeResult
            [
                "clrstack";
                "clrthreads";
                "clrmodules";
                "eeheap";
                "dumpheap";
                "dso";
                "eeversion";
                "exit";
            ]
            |> List.map (fun sosCommand -> analyzeInvoke.Proc.StandardInput.WriteLine(sosCommand))
            |> ignore

            analyzeInvoke.Proc.WaitForExit()
            yield! analyzeInvokeResult
        }