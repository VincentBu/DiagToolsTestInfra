namespace DiagToolsValidator.Command

open System.IO

open Spectre.Console
open Spectre.Console.Cli

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Task.LTTng

module LTTngTestRun =
    type LTTngTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set

    type LTTngTestRunCommand() =
        inherit Command<LTTngTestRunSettings>()
        let _baseGCPerfsimSrcPath = Path.Combine("Commands", "LTTngTest", "TargetApps", "gcperfsim", "Program.cs.txt")
        
        override this.Execute(context: CommandContext, setting: LTTngTestRunSettings) =
            AnsiConsole.Write((new FigletText("LTTng Test")).Centered().Color(Color.Red))
            let aggregatedConfig = 
                LTTngTestRunConfiguration.LTTngTestConfigurationGenerator.GenerateRunConfiguration setting.ConfigurationPath
            
            aggregatedConfig.RunConfigurationList
            |> List.map (
                fun configuration ->
                    // Create analysis output folder and dump folder
                    Core.CreateDirectory configuration.TestResultFolder |> ignore
                    
                    let loggerPath = Path.Combine(configuration.TestBed, "Initialization.txt")
                    let trace = new Core.ProgressTraceBuilder(loggerPath)
                    
                    trace {
                        // Generate environment activation script
                        (TestInfrastructure.GenerateEnvironmentActivationScript configuration) |> ignore

                        // Install .NET SDK
                        AnsiConsole.Write(new Rule($"Install .NET SDK {configuration.DotNet.SDKVersion}"))
                        AnsiConsole.WriteLine()
                        trace.AppendLineToLogger $"Install .NET SDK {configuration.DotNet.SDKVersion} to {configuration.DotNet.DotNetRoot}"
                        let! dotNetRootInfo = DotNet.InstallDotNetSDKByVersion DotNet.CurrentRID 
                                                                               configuration.DotNet.SDKVersion 
                                                                               configuration.DotNet.DotNetRoot

                        // Prepare oom app and uhe app for testing (create, modify source code and project file, publish)
                        AnsiConsole.Write(new Rule($"Prepare .NET apps for testing({configuration.DotNet.SDKVersion})"))
                        AnsiConsole.WriteLine()

                        trace.AppendLineToLogger $"Create gcperfsim app"
                        yield! configuration.TargetApp.GCPerfsim.CreateApp()

                        // Replace source file
                        let targetNativeAOTAppSrcPath = Path.Combine(configuration.TargetApp.GCPerfsim.AppRoot, "Program.cs")
                        let consoleSrcContent = File.ReadAllText(_baseGCPerfsimSrcPath)
                        File.WriteAllText(targetNativeAOTAppSrcPath, consoleSrcContent)

                        trace.AppendLineToLogger $"Build gcperfsim app"
                        yield! configuration.TargetApp.GCPerfsim.BuildApp configuration.TargetApp.BuildConfig DotNet.CurrentRID
                        
                        // Download Perfcollect
                        DotNetTool.DownloadPerfcollect configuration.PerfCollectPath |> ignore
                    } |> ignore
                    
                    // Test Cross OS DAC
                    AnsiConsole.Write(new Rule($"Test Cross OS DAC for {configuration.DotNet.SDKVersion}"))
                    AnsiConsole.WriteLine()
                    LTTng.TestLTTng configuration)
            |> ignore
            
            0

