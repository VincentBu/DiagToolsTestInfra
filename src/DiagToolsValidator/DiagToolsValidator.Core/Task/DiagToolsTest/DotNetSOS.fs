namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System
open System.IO

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.Functionality

module DotNetSOS =
    let BaseSOSCommandList = [
        "clrstack";
        "clrthreads";
        "clrmodules";
        "eestack";
        "eeheap";
        "dumpstack";
        "dumpheap";
        "dso";
        "eeversion";
    ]


    let TestDotNetSOS (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-sos"
        
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)

        let result = trace {
            let! toolILPath = DotNetTool.GetToolIL configuration.DiagTool.ToolRoot toolName configuration.DiagTool.DiagToolVersion

            for arguments in [
                $"{toolILPath} --help";
                $"{toolILPath} install";
                $"{toolILPath} uninstall";
                $"{toolILPath} install";
            ] do
                yield! DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                               arguments
                                               configuration.TestResultFolder
                                               true
                                               CommandLineTool.PrinteOutputData
                                               CommandLineTool.PrintErrorData
                                               true
        }
        
        // Attach to webapp
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}-debug-process.txt")
        let processDebuggingTrace = new Core.ProgressTraceBuilder(loggerPath)
        processDebuggingTrace {
            let debuggerScriptPath = Path.Combine(configuration.TestBed, "debug-script.txt")
            let! debugProcessScript = Debugging.GenerateDebugScript BaseSOSCommandList debuggerScriptPath

            let webappInvokerResult = TestInfrastructure.RunWebapp(configuration)
            yield! webappInvokerResult

            let! webappInvoker = webappInvokerResult

            let debugInvoker = Debugging.DebugAttachedProcessWithSOS configuration.SystemInfo.CLIDebugger 
                                                           configuration.SystemInfo.EnvironmentVariables 
                                                           debugProcessScript 
                                                           (webappInvoker.Proc.Id.ToString())

            CommandLineTool.TerminateCommandInvoker(webappInvoker) |> ignore
            yield! debugInvoker
        } |> ignore
        
        // Debug dump
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}-debug-dump.txt")
        let dumpDebuggingTrace = new Core.ProgressTraceBuilder(loggerPath)
        dumpDebuggingTrace {
            let debuggerScriptPath = Path.Combine(configuration.TestBed, "debug-script.txt")
            let! debugDumpScript = Debugging.GenerateDebugScript BaseSOSCommandList debuggerScriptPath

            let dumpPath = 
                Directory.GetFiles(configuration.TestBed, "webapp*.dmp")
                |> Array.head
            yield! Debugging.DebugDumpWithSOS configuration.SystemInfo.CLIDebugger
                                              configuration.SystemInfo.EnvironmentVariables 
                                              ""
                                              debugDumpScript
                                              dumpPath
        } |> ignore

        result