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
                let! toolILPath = DotNetTool.GetToolIL configuration.DebuggerExtension.ToolRoot 
                                                       toolName 
                                                       configuration.DebuggerExtension.ToolVersion
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
        let dumpDebugResultFolder = Path.Combine(configuration.TestResultFolder, "DebugDump")
        Core.CreateDirectory dumpDebugResultFolder |> ignore

        let dumpDebuggingLoggerPath = Path.Combine(configuration.TestResultFolder, $"dump-debugging.txt")
        let dumpDebuggingTrace = new Core.ProgressTraceBuilder(dumpDebuggingLoggerPath)
        dumpDebuggingTrace {
            let debuggerScriptPath = Path.Combine(dumpDebugResultFolder, "debug-script.txt")
            let! debugDumpScript = Debugging.GenerateDebugScript SOSCommandList debuggerScriptPath
            // Initialize dump generating env
            let! env = DotNet.ActiveNativeDumpGeneratingEnvironment configuration.SystemInfo.EnvironmentVariables 
                                                                          configuration.TestResultFolder
            let env = DotNet.ActiveStressLogEnvironment env

            let! srcCreateDumpPath = configuration.TargetApp.NativeAOTApp.GetCreateDump configuration.TargetApp.BuildConfig DotNet.CurrentRID
            let! nativeSymbolFolder = configuration.TargetApp.NativeAOTApp.GetNativeSymbolFolder configuration.TargetApp.BuildConfig DotNet.CurrentRID
            Core.CopyFile srcCreateDumpPath nativeSymbolFolder |> ignore

            // Run nativeaot app
            let! executablePath = configuration.TargetApp.NativeAOTApp.GetAppNativeExecutable configuration.TargetApp.BuildConfig DotNet.CurrentRID
            CommandLineTool.RunCommand executablePath
                                       ""
                                       ""
                                       env
                                       true
                                       CommandLineTool.IgnoreOutputData
                                       CommandLineTool.IgnoreErrorData
                                       true
            |> ignore

            let! dumpPath = TestInfrastructure.GetDump configuration
            // Debug dump
            yield! Debugging.DebugDumpWithSOS configuration.SystemInfo.CLIDebugger
                                              configuration.SystemInfo.EnvironmentVariables
                                              dumpDebugResultFolder
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
            let! executablePath = configuration.TargetApp.NativeAOTApp.GetAppNativeExecutable configuration.TargetApp.BuildConfig DotNet.CurrentRID
            let env = DotNet.ActiveStressLogEnvironment configuration.SystemInfo.EnvironmentVariables
            yield! Debugging.DebugLaunchableWithSOS configuration.SystemInfo.CLIDebugger
                                                    env
                                                    resultFolderInfo.FullName
                                                    debugLaunchableScript
                                                    executablePath
        } |> ignore

        DotNet.DeactiveNativeDumpGeneratingEnvironment configuration.SystemInfo.EnvironmentVariables |> ignore

        result
