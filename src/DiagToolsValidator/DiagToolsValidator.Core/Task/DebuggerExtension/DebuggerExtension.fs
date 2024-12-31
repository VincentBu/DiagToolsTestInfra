namespace DiagToolsValidator.Core.Task.DebuggerExtension

open System.IO

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration

module DebuggerExtension =
    let SOSCommandList = 
        [
            "clrthreads";
            "verifyheap";
            "dumpheap -stat";
            "dumpasync";
            "dumplog";
            "crashinfo";
        ]

    let TestDebuggerExtension (configuration: DebuggerExtensionTestConfiguration.DebuggerExtensionTestRunConfiguration) =
        let toolName = "dotnet-debugger-extensions"
        
        let loggerPath = Path.Combine(configuration.TestResultFolder, $"{toolName}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)

        // Install debugger-extension
        let result = 
            trace {
                let! toolILPath = DotNetTool.GetToolIL configuration.DebuggerExtension.ToolRoot toolName configuration.DebuggerExtension.ToolVersion
                trace.AppendLineToLogger($"Install {toolName}")
                yield! DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                                $"{toolILPath} install --accept-license-agreement"
                                                configuration.TestResultFolder
                                                true
                                                CommandLineTool.IgnoreErrorData
                                                CommandLineTool.IgnoreErrorData
                                                true
            }
        
        // Debug dump
        let dumpDebuggingLoggerPath = Path.Combine(configuration.TestResultFolder, $"dump-debugging.txt")
        let dumpDebuggingTrace = new Core.ProgressTraceBuilder(dumpDebuggingLoggerPath)
        let resultFolder = Path.Combine(configuration.TestResultFolder, "DebugDump")
        dumpDebuggingTrace {
            let! resultFolderInfo = Core.CreateDirectory resultFolder
            let debuggerScriptPath = Path.Combine(resultFolderInfo.FullName, "debug-script.txt")
            let! debugDumpScript = Debugging.GenerateDebugScript SOSCommandList debuggerScriptPath
            // Generate dump
            let! dumpPath = TestInfrastructure.RunNativeAOTApp configuration
            yield! Debugging.DebugDumpWithSOS configuration.SystemInfo.CLIDebugger
                                              configuration.SystemInfo.EnvironmentVariables
                                              resultFolderInfo.FullName
                                              debugDumpScript
                                              dumpPath
        } |> ignore

        // Debug Launchable
        let launchableDebuggingLoggerPath = Path.Combine(configuration.TestResultFolder, $"launchable-debugging.txt")
        let launchableDebuggingTrace = new Core.ProgressTraceBuilder(launchableDebuggingLoggerPath)
        let resultFolder = Path.Combine(configuration.TestResultFolder, "DebugLaunchable")
        launchableDebuggingTrace {
            let! resultFolderInfo = Core.CreateDirectory resultFolder
            let debuggerScriptPath = Path.Combine(resultFolderInfo.FullName, "debug-script.txt")
            let! debugLaunchableScript = Debugging.GenerateDebugScript SOSCommandList debuggerScriptPath
            let! executablePath = configuration.TargetApp.NativeAOTApp.GetAppNativeExecutable configuration.TargetApp.BuildConfig
            let env = TestInfrastructure.ActiveStressLogEnvironment configuration.SystemInfo.EnvironmentVariables
            yield! Debugging.DebugLaunchableWithSOS configuration.SystemInfo.CLIDebugger
                                                    env
                                                    resultFolderInfo.FullName
                                                    debugLaunchableScript
                                                    executablePath
        } |> ignore
        result
