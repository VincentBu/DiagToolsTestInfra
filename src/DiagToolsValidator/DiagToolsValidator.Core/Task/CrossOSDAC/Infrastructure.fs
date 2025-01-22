namespace DiagToolsValidator.Core.Task.CrossOSDAC

open System.IO

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration
open System
open System.Threading


module TestInfrastructure =
    let GenerateEnvironmentActivationScript (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
        let scriptPath, lines =
            if DotNet.CurrentRID.Contains "win"
            then Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.ps1"), [
                $"$Env:DOTNET_ROOT=\"{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DotNetDump.ToolRoot}\""
            ]
            else Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.sh"),[
                $"export DOTNET_ROOT={configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DotNetDump.ToolRoot}"
            ]

        try
            File.WriteAllLines(scriptPath, lines)
            Choice1Of2 scriptPath
        with ex -> 
            ex.Data.Add("", $"Fail to generate environment activation script for SDK-{configuration.DotNet.SDKVersion}.")
            Choice2Of2 ex


    let GenerateDumpForOOM (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)
        let invokeResult = 
            trace {
                let dumpPath = Path.Combine(configuration.DumpFolder, $"dump-oom-{configuration.DotNet.SDKVersion}-{DotNet.CurrentRID}.dmp")
                let dumpGenerateEnv = DotNet.ActiveDumpGeneratingEnvironment configuration.SystemInfo.EnvironmentVariables dumpPath
                let! executablePath = configuration.TargetApp.OOMApp.GetAppExecutable configuration.TargetApp.BuildConfig DotNet.CurrentRID
                let! commandInvoker = CommandLineTool.RunCommand executablePath
                                                                 ""
                                                                 ""
                                                                 dumpGenerateEnv
                                                                 true
                                                                 CommandLineTool.PrinteOutputData
                                                                 CommandLineTool.PrintErrorData
                                                                 true
                return commandInvoker
            }

        invokeResult
        

    let GenerateDumpForUHE (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)
        let invokeResult = 
            trace {
                let dumpPath = Path.Combine(configuration.DumpFolder, $"dump-uhe-{configuration.DotNet.SDKVersion}-{DotNet.CurrentRID}.dmp")
                let dumpGenerateEnv = DotNet.ActiveDumpGeneratingEnvironment configuration.SystemInfo.EnvironmentVariables dumpPath
                let! executablePath = configuration.TargetApp.UHEApp.GetAppExecutable configuration.TargetApp.BuildConfig DotNet.CurrentRID
                let! commandInvoker = CommandLineTool.RunCommand executablePath
                                                                 ""
                                                                 ""
                                                                 dumpGenerateEnv
                                                                 true
                                                                 CommandLineTool.PrinteOutputData
                                                                 CommandLineTool.PrintErrorData
                                                                 true
                return commandInvoker
            }

        invokeResult


    let FilterDumpByTargetRID (targetRID: string) (dumpPath: string) =
        let dumpName = Path.GetFileNameWithoutExtension(dumpPath)
        if dumpName.EndsWith("x64") || dumpName.EndsWith("arm64")
        then "win-x64" = targetRID
        else "win-x86" = targetRID