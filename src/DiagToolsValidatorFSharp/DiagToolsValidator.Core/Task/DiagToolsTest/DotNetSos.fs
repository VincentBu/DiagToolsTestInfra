﻿namespace DiagToolsValidator.Core.Task.DiagToolsTest

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


    let GenerateDebugScript (debugOutput: string) (scriptRoot) =
        let debugCommandList = 
            if DotNet.CurrentRID.Contains("win")
            then
                let userProfile = Environment.GetEnvironmentVariable("USERPROFILE")
                let sosPluginPath = Path.Combine(userProfile, ".dotnet", "sos", "sos.dll")
                let preRunCommandList =
                    [
                        ".unload sos";
                        "sxe ld coreclr";
                        $".load {sosPluginPath}";
                        $".logopen {debugOutput}"
                    ]

                let winSOSCommandList = BaseSOSCommandList |> List.map (fun command -> $"!{command}")
                    
                let exitCommandList =
                    [
                        ".logclose";
                        ".detach";
                        "qq";
                    ]
                     
                [preRunCommandList; winSOSCommandList; exitCommandList]
                |> List.concat

            else
                
                let preRunCommandList =
                    [
                        $"log enable -f {debugOutput} lldb all"
                    ]
                let exitCommandList =
                    [
                        "exit";
                    ]
                [preRunCommandList; BaseSOSCommandList; exitCommandList]
                |> List.concat
        
        try
            let debuggerScriptPath = Path.Combine(scriptRoot, "windbg-debug-script.txt")
            File.WriteAllLines(debuggerScriptPath, debugCommandList)
            Choice1Of2 debuggerScriptPath
        with ex -> Choice2Of2 ex


    let DebugDumpWithSOS (debugger: string) (dotNetRoot: string) (debuggerScriptPath: string) (dumpPath: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-cf {debuggerScriptPath} -z {dumpPath}"
            else $"-c {dumpPath} -s {debuggerScriptPath}"

        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- dotNetRoot
        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   "" 
                                   env 
                                   true 
                                   CommandLineTool.IgnoreOutputData 
                                   CommandLineTool.IgnoreErrorData 
                                   true


    let DebugAttachedProcessWithSOS (debugger: string) (dotNetRoot: string) (debuggerScriptPath: string) (pid: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-cf {debuggerScriptPath} -p {pid}"
            else $"-s {debuggerScriptPath} -p {pid}"

        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- dotNetRoot
        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   "" 
                                   env 
                                   true 
                                   CommandLineTool.IgnoreOutputData 
                                   CommandLineTool.IgnoreErrorData 
                                   true

    let TestDotNetSos (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let toolName = "dotnet-sos"
        
        let env = new Dictionary<string, string>()
        env["DOTNTE_ROOT"] <- configuration.DotNet.DotNetRoot
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)

        trace {
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
            
            // Attach to webapp
            let! debugProcessScript = GenerateDebugScript (Path.Combine(configuration.TestResultFolder, "debug-process.txt"))
                                                          configuration.TestBed

            let webappInvokerResult = TestInfrastructure.RunWebapp(configuration)
            yield! webappInvokerResult

            let! webappInvoker = webappInvokerResult

            yield! DebugAttachedProcessWithSOS configuration.SystemInfo.CLIDebugger 
                                               configuration.DotNet.DotNetRoot 
                                               debugProcessScript 
                                               (webappInvoker.Proc.Id.ToString())

            CommandLineTool.TerminateCommandInvoker(webappInvoker) |> ignore

            // Debug dump
            let! debugDumpScript = GenerateDebugScript (Path.Combine(configuration.TestResultFolder, "debug-dump.txt"))
                                                       configuration.TestBed

            let dumpPath = 
                Directory.GetFiles(configuration.TestBed, "webapp*.dmp")
                |> Array.head
            yield! DebugDumpWithSOS configuration.SystemInfo.CLIDebugger
                                    configuration.DotNet.DotNetRoot
                                    debugDumpScript
                                    dumpPath
        }