namespace DiagToolsValidator.Core.Task.CrossOSDAC

open System.IO

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration

module CrossOSDAC =
    let BaseSOSCommandList = 
        [
            "clrstack";
            "clrstack -i";
            "clrthreads";
            "clrmodules";
            "eeheap";
            "dumpheap";
            "printexception";
            "dso";
            "eeversion";
            "exit";
        ]


    let ExtractTargetRIDFromAppName (appName: string) =
        if appName.EndsWith("linux-x64")
        then "linux-x64"
        elif appName.EndsWith("linux-musl-x64")
        then "linux-musl-x64"
        elif appName.EndsWith("linux-arm64")
        then "linux-arm64"
        elif appName.EndsWith("linux-musl-arm64")
        then "linux-musl-arm64"
        elif appName.EndsWith("linux-arm")
        then "linux-arm"
        elif appName.EndsWith("linux-musl-arm")
        then "linux-musl-arm"
        else ""


    let AnalyzeDumpOnLinux (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
        let toolName = "dotnet-dump"
        Directory.GetFiles(configuration.DumpFolder, $"*.dmp")
        |> Array.map(
            fun dumpPath ->
                let dumpName = Path.GetFileNameWithoutExtension(dumpPath)
                let loggerPath = Path.Combine(configuration.AnalysisOutputFolder, $"output_{dumpName}.txt")
                let trace = new Core.ProgressTraceBuilder(loggerPath)
                trace {
                    let! toolILPath = DotNetTool.GetToolIL configuration.DotNetDump.ToolRoot toolName configuration.DotNetDump.ToolVersion
                    let analyzeInvokeResult = DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                                                      $"{toolILPath} analyze {dumpPath}"
                                                                      configuration.AnalysisOutputFolder
                                                                      true
                                                                      CommandLineTool.IgnoreOutputData
                                                                      CommandLineTool.IgnoreErrorData
                                                                      false
                    let! analyzeInvoke = analyzeInvokeResult
                    BaseSOSCommandList
                    |> List.map (fun sosCommand -> analyzeInvoke.Proc.StandardInput.WriteLine(sosCommand))
                    |> ignore

                    analyzeInvoke.Proc.WaitForExit()
                    yield! analyzeInvokeResult
                })


    let AnalyzeDumpOnWindows (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) (targetRID: string) =
        let toolName = "dotnet-dump"
        Directory.GetFiles(configuration.DumpFolder, $"*.dmp")
        |> Array.filter(fun dumpPath -> TestInfrastructure.FilterDumpByTargetRID targetRID dumpPath)
        |> Array.map(
            fun dumpPath ->
                let dumpName = Path.GetFileNameWithoutExtension(dumpPath)
                let appRoot = Path.Combine(configuration.TestAssets, "TargetApps", configuration.DotNet.SDKVersion, dumpName)
                let _rid = ExtractTargetRIDFromAppName appRoot
                let loggerPath = Path.Combine(configuration.AnalysisOutputFolder, $"output_{dumpName}_win.txt")
                let trace = new Core.ProgressTraceBuilder(loggerPath)
                trace {
                    let! toolILPath = DotNetTool.GetToolIL configuration.DotNetDump.ToolRoot toolName configuration.DotNetDump.ToolVersion
                    let analyzeInvokeResult = DotNet.RunDotNetCommand configuration.SystemInfo.EnvironmentVariables
                                                                      $"{toolILPath} analyze {dumpPath}"
                                                                      configuration.AnalysisOutputFolder
                                                                      true
                                                                      CommandLineTool.IgnoreOutputData
                                                                      CommandLineTool.IgnoreErrorData
                                                                      false
                    let! analyzeInvoke = analyzeInvokeResult
                    let! symbolFolder = DotNetApp.GetSymbolFolder appRoot configuration.TargetApp.BuildConfig _rid
                    $"setsymbolserver -directory {symbolFolder}"::BaseSOSCommandList
                    |> List.map (fun sosCommand -> analyzeInvoke.Proc.StandardInput.WriteLine(sosCommand))
                    |> ignore

                    analyzeInvoke.Proc.WaitForExit()
                    yield! analyzeInvokeResult
                })


    let TestCrossOSDAC (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) (targetRID: string) =
        if DotNet.CurrentRID.Contains("win")
        then
            AnalyzeDumpOnWindows configuration targetRID
        else
            AnalyzeDumpOnLinux configuration