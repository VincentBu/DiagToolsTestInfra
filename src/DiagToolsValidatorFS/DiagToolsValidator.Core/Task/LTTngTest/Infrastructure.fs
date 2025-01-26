namespace DiagToolsValidator.Core.Task.LTTng

open System.IO
open System.Collections.Generic

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration


module TestInfrastructure =
    let GenerateEnvironmentActivationScript (configuration: LTTngTestRunConfiguration.LTTngTestRunConfiguration) =
        let scriptPath, lines =
            if DotNet.CurrentRID.Contains "win"
            then Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.ps1"), [
                $"$Env:DOTNET_ROOT=\"{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DotNet.DotNetRoot}\"";
            ]
            else Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.sh"),[
                $"export DOTNET_ROOT={configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DotNet.DotNetRoot}";
            ]

        try
            File.WriteAllLines(scriptPath, lines)
            Choice1Of2 scriptPath
        with ex -> 
            ex.Data.Add("", $"Fail to generate environment activation script for SDK-{configuration.DotNet.SDKVersion}.")
            Choice2Of2 ex


    let RunGCPerfsim (configuration: LTTngTestRunConfiguration.LTTngTestRunConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)
        trace {
            let! executablePath = configuration.TargetApp.GCPerfsim.GetAppExecutable configuration.TargetApp.BuildConfig DotNet.CurrentRID
            let _env = new Dictionary<string, string>(configuration.SystemInfo.EnvironmentVariables)
            _env["DOTNET_PerfMapEnabled"] <- "1"
            _env["DOTNET_EnableEventLog"] <- "1"

            let! commandInvoker = CommandLineTool.RunCommand executablePath
                                                             ""
                                                             configuration.TestResultFolder
                                                             _env
                                                             true
                                                             CommandLineTool.IgnoreOutputData
                                                             CommandLineTool.IgnoreErrorData
                                                             false
            return commandInvoker
        }
        

    let RunPerfcollect (configuration: LTTngTestRunConfiguration.LTTngTestRunConfiguration) =
        let tracePath = Path.Combine(
            configuration.TestResultFolder, $"trace_net{configuration.DotNet.SDKVersion}_{DotNet.CurrentRID}")
        CommandLineTool.RunCommand "/bin/bash" 
                                   $"{configuration.PerfCollectPath} collect {tracePath} -collectsec 30" 
                                   configuration.TestResultFolder 
                                   configuration.SystemInfo.EnvironmentVariables 
                                   true 
                                   CommandLineTool.IgnoreOutputData 
                                   CommandLineTool.IgnoreErrorData 
                                   true
                        