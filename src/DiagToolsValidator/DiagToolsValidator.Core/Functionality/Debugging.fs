namespace DiagToolsValidator.Core.Functionality

open System
open System.IO
open System.Collections.Generic

module Debugging =
    let GenerateDebugScript (sosCommandList: string list) (scriptPath: string) =
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

                let winSOSCommandList = sosCommandList |> List.map (fun command -> $"!{command}")
                    
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
                [preRunCommandList; sosCommandList; exitCommandList]
                |> List.concat
        
        try
            File.WriteAllLines(scriptPath, debugCommandList)
            Choice1Of2 scriptPath
        with ex -> Choice2Of2 ex


    let DebugDumpWithSOS (debugger: string) 
                         (env: Dictionary<string, string>)
                         (workingDirectory: string)
                         (debuggerScriptPath: string)
                         (dumpPath: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-cf {debuggerScriptPath} -z {dumpPath}"
            else $"-c {dumpPath} -s {debuggerScriptPath}"

        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   workingDirectory 
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
                                   
    let DebugLaunchableWithSOS (debugger: string) 
                               (env: Dictionary<string, string>)
                               (workingDirectory: string)
                               (debuggerScriptPath: string)
                               (launchable: string) =
        let arguments = 
            if DotNet.CurrentRID.Contains("win")
            then $"-g -cf {debuggerScriptPath} {launchable}"
            else $"lldb -s {debuggerScriptPath} -o \"run\" {launchable}"

        let _env = new Dictionary<string, string>(env)
        _env["DOTNET_StressLogLevel"] <- "10"
        _env["DOTNET_TotalStressLogSize"] <- "8196"
        CommandLineTool.RunCommand debugger 
                                   arguments 
                                   workingDirectory 
                                   _env 
                                   true 
                                   CommandLineTool.IgnoreOutputData
                                   CommandLineTool.IgnoreErrorData
                                   true
