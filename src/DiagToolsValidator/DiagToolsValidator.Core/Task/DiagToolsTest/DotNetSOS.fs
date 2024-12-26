namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System
open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality

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


    let GenerateDebugScript (scriptRoot) =
        let debugCommandList = 
            if DotNet.CurrentRID.Contains("win")
            then
                let userProfile = Environment.GetEnvironmentVariable("USERPROFILE")
                let sosPluginPath = Path.Combine(userProfile, ".dotnet", "sos", "sos.dll")
                let preRunCommandList =
                    [
                        ".unload sos";
                        $".load {sosPluginPath}";
                    ]

                let winSOSCommandList = BaseSOSCommandList |> List.map (fun command -> $"!{command}")
                    
                let exitCommandList =
                    [
                        ".detach";
                        "qq";
                    ]
                     
                [preRunCommandList; winSOSCommandList; exitCommandList]
                |> List.concat

            else
                let preRunCommandList = List.Empty
                let exitCommandList =
                    [
                        "exit";
                    ]
                [preRunCommandList; BaseSOSCommandList; exitCommandList]
                |> List.concat
        
        try
            let debuggerScriptPath = Path.Combine(scriptRoot, "debug-script.txt")
            File.WriteAllLines(debuggerScriptPath, debugCommandList)
            Choice1Of2 debuggerScriptPath
        with ex -> Choice2Of2 ex


    let DebugDumpWithSOS (debugger: string) (env: Dictionary<string, string>) (debuggerScriptPath: string) (dumpPath: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-cf {debuggerScriptPath} -z {dumpPath}"
            else $"-c {dumpPath} -s {debuggerScriptPath}"

        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   "" 
                                   env 
                                   true 
                                   CommandLineTool.IgnoreOutputData 
                                   CommandLineTool.IgnoreErrorData 
                                   true


    let DebugAttachedProcessWithSOS (debugger: string) 
                                    (env: Dictionary<string, string>)
                                    (debuggerScriptPath: string)
                                    (pid: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-cf {debuggerScriptPath} -p {pid}"
            else $"-s {debuggerScriptPath} -p {pid}"

        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   "" 
                                   env 
                                   true 
                                   CommandLineTool.IgnoreOutputData 
                                   CommandLineTool.IgnoreErrorData 
                                   true


    let TestDotNetSOS (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-sos"
        
        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- configuration.DotNet.DotNetRoot
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
            let! debugProcessScript = GenerateDebugScript configuration.TestBed

            let webappInvokerResult = TestInfrastructure.RunWebapp(configuration)
            yield! webappInvokerResult

            let! webappInvoker = webappInvokerResult

            let debugInvoker = DebugAttachedProcessWithSOS configuration.SystemInfo.CLIDebugger 
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
            let! debugDumpScript = GenerateDebugScript configuration.TestBed

            let dumpPath = 
                Directory.GetFiles(configuration.TestBed, "webapp*.dmp")
                |> Array.head
            yield! DebugDumpWithSOS configuration.SystemInfo.CLIDebugger
                                    configuration.SystemInfo.EnvironmentVariables 
                                    debugDumpScript
                                    dumpPath
        } |> ignore

        result