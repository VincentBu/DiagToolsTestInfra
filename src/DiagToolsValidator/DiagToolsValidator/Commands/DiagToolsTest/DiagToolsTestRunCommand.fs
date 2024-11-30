namespace DiagToolsValidationToolSet.Command

open System.IO
open System.Runtime.InteropServices
open Spectre.Console
open Spectre.Console.Cli
open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality
open DiagToolsTestConfiguration
open DotNetApp
open System.Collections.Generic
open DiagToolsValidator.Core.Task

module DiagToolsTestRun =
    type DiagToolsTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set


    type DiagToolsTestRunCommand() =
        inherit Command<DiagToolsTestRunSettings>()

        let DiagToolCollection = [
            "dotnet-counters";
            "dotnet-dump";
            "dotnet-gcdump";
            "dotnet-sos";
            "dotnet-stack";
            "dotnet-trace";]

        override this.Execute(context: CommandContext, setting: DiagToolsTestRunSettings) =
            AnsiConsole.Write(new Rule("Start Diagnostic Tools Test"));
            AnsiConsole.WriteLine();

            let GenerateEnvironmentActivationScript (configuration: DiagToolsTestConfiguration.DiagToolsTestRunConfiguration) =
                let scriptPath, lines =
                    if RuntimeInformation.RuntimeIdentifier.Contains "win"
                    then Path.Combine(configuration.TestBed, "env_activation.ps1"), [
                        $"$Env:DOTNET_ROOT={configuration.DotnetRoot}";
                        $"$Env:Path+=;{configuration.DotnetRoot}";
                        $"$Env:Path+=;{configuration.DiagToolRoot}"
                    ]
                    else Path.Combine(configuration.TestBed, "env_activation.sh"),[
                        $"export DOTNET_ROOT={configuration.DotnetRoot}";
                        $"export PATH=$PATH:{configuration.DotnetRoot}";
                        $"export PATH=$PATH:{configuration.DiagToolRoot}"
                    ]

                try
                    Choice1Of2 (File.WriteAllLines(scriptPath, lines))
                with ex -> Choice2Of2 (new exn("Fail to generate environment activation script."))

            //let monitor = new ComputationExpressionBuilder.FunctionMonitorBuilder()

            //monitor {
            //    let! baseConfiguration = DiagToolsTestConfiguration.DiagToolsTestRunConfigurationGenerator.ParseConfigFile setting.ConfigurationPath
            //    let! configuration = DiagToolsTestConfiguration.DiagToolsTestRunConfigurationGenerator.GenerateConfiguration baseConfiguration
                
            //    // Create testbed and test result folder
            //    let! testbedInfo = Common.CreateDirectory configuration.TestBed
            //    let! resultFolderInfo = Common.CreateDirectory configuration.TestResultFolder

            //    // Copy configuration file to test result folder
            //    let! copyConfRes = Common.CopyFile setting.ConfigurationPath configuration.TestResultFolder

            //    // Generate environment activation script
            //    let! createScriptRes = GenerateEnvironmentActivationScript configuration

            //    // Install .NET SDK
            //    let scriptPath = 
            //        if RuntimeInformation.RuntimeIdentifier.Contains "win"
            //        then Path.Combine(configuration.TestBed, "dotnet-install.ps1")
            //        else Path.Combine(configuration.TestBed, "dotnet-install.sh")

            //    let! scriptPath = 
            //        DotNetSDKAndRuntime.DownloadInstallScript scriptPath
            //        |> Async.RunSynchronously

            //    let! scriptPath = DotNetSDKAndRuntime.MakeScriptRunnable scriptPath
            //    let! dotnetRoot = DotNetSDKAndRuntime.DownloadSDKWithScript
            //                        scriptPath 
            //                        configuration.DotnetRoot 
            //                        configuration.SDKVersion

            //    // Install diagnostic tools
            //    let dotnetBin = Path.Combine(dotnetRoot, "dotnet")
            //    let InstallDiagnosticTools = DotNetTool.InstallDotNetTool 
            //                                    configuration.EnvironmentVariable
            //                                    dotnetBin
            //                                    configuration.DiagToolRoot
            //                                    configuration.DiagTool.Feed
            //                                    configuration.DiagTool.DiagToolVersion

            //    let toolInstallResult =
            //        DiagToolCollection
            //        |> List.map (fun toolName -> toolName, (InstallDiagnosticTools toolName))
            //        |> Map.ofList

            //    // Create target .NET app
            //    let! consoleappBin = TargetApp.CreateBuildConsoleApp configuration
            //    let! webappBin = TargetApp.CreateBuildWebApp configuration
            //    let! gcDumpPlaygroundBin = TargetApp.CreateBuildGCDumpPlayground configuration

            //    // Run the test

            //    return None
            //} |> ignore
            
            0