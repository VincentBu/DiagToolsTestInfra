namespace DiagToolsValidator.Command

open System.IO

open Spectre.Console
open Spectre.Console.Cli

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Task.CrossOSDAC

module CrossOSDACTestRun=
    type CrossOSDACTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set

    type CrossOSDACTestRunCommand() =
        inherit Command<CrossOSDACTestRunSettings>()
        let _baseOOMAppSrcPath = Path.Combine("Commands", "CrossOSDACTest", "TargetApps", "oom", "Program.cs.txt")
        let _baseUHESrcPath = Path.Combine("Commands", "CrossOSDACTest", "TargetApps", "uhe", "Program.cs.txt")
        
        override this.Execute(context: CommandContext, setting: CrossOSDACTestRunSettings) =
            AnsiConsole.Write((new FigletText("Cross OS DAC Test")).Centered().Color(Color.Red))

            let aggregatedConfig = 
                CrossOSDACTestRunConfiguration.CrossOSDACTestConfigurationGenerator.GenerateRunConfiguration setting.ConfigurationPath
            
            if DotNet.CurrentRID.Contains("win")
            then
                aggregatedConfig.RunConfigurationList
                |> List.map (fun configuration -> this.RunOnWindows configuration)
                |> ignore
            else
                aggregatedConfig.RunConfigurationList
                |> List.map (fun configuration -> this.RunOnLinux configuration)
                |> ignore

            0

        member this.RunOnLinux (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
            // Create analysis output folder and dump folder
            Core.CreateDirectory configuration.AnalysisOutputFolder |> ignore
            Core.CreateDirectory configuration.DumpFolder |> ignore
                    
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

                trace.AppendLineToLogger $"Create OOM app"
                yield! configuration.TargetApp.OOMApp.CreateApp()

                // Replace source file
                let targetNativeAOTAppSrcPath = Path.Combine(configuration.TargetApp.OOMApp.AppRoot, "Program.cs")
                let consoleSrcContent = File.ReadAllText(_baseOOMAppSrcPath)
                File.WriteAllText(targetNativeAOTAppSrcPath, consoleSrcContent)

                trace.AppendLineToLogger $"Build OOM app"
                yield! configuration.TargetApp.OOMApp.BuildApp configuration.TargetApp.BuildConfig DotNet.CurrentRID
                        
                trace.AppendLineToLogger $"Create UHE app"
                yield! configuration.TargetApp.UHEApp.CreateApp()

                // Replace source file
                let targetNativeAOTAppSrcPath = Path.Combine(configuration.TargetApp.UHEApp.AppRoot, "Program.cs")
                let consoleSrcContent = File.ReadAllText(_baseUHESrcPath)
                File.WriteAllText(targetNativeAOTAppSrcPath, consoleSrcContent)

                trace.AppendLineToLogger $"Build UHE app"
                yield! configuration.TargetApp.UHEApp.BuildApp configuration.TargetApp.BuildConfig DotNet.CurrentRID

                // Generate dumps for oom and uhe apps
                TestInfrastructure.GenerateDumpForOOM configuration |> ignore
                TestInfrastructure.GenerateDumpForUHE configuration |> ignore
                        
                // Install dotnet-dump
                AnsiConsole.Write(new Rule($"Install dotnet-dump({configuration.DotNet.SDKVersion})"))
                AnsiConsole.WriteLine()
                let toolName = "dotnet-dump"
                trace.AppendLineToLogger $"Install {toolName} {configuration.DotNetDump.ToolVersion} to {configuration.DotNetDump.ToolRoot}"
                yield! DotNetTool.InstallDotNetTool configuration.SystemInfo.EnvironmentVariables
                                                    configuration.DotNetDump.ToolRoot
                                                    configuration.DotNetDump.Feed 
                                                    ""
                                                    configuration.DotNetDump.ToolVersion 
                                                    toolName

            } |> ignore
                    
            // Test Cross OS DAC
            AnsiConsole.Write(new Rule($"Test Cross OS DAC for {configuration.DotNet.SDKVersion}"))
            AnsiConsole.WriteLine()
            CrossOSDAC.TestCrossOSDAC configuration DotNet.CurrentRID

        member this.RunOnWindows (configuration: CrossOSDACTestRunConfiguration.CrossOSDACTestRunConfiguration) =
            // Create analysis output folder and dump folder
            Core.CreateDirectory configuration.AnalysisOutputFolder |> ignore
            Core.CreateDirectory configuration.DumpFolder |> ignore
                    
            let loggerPath = Path.Combine(configuration.TestBed, "Initialization.txt")
            let trace = new Core.ProgressTraceBuilder(loggerPath)
            
            ["win-x64"; "win-x86"]
            |> List.map(
                fun targetRID ->
                    trace {
                        // Generate environment activation script
                        (TestInfrastructure.GenerateEnvironmentActivationScript configuration) |> ignore

                        // Install .NET SDK
                        AnsiConsole.Write(new Rule($"Install .NET SDK {configuration.DotNet.SDKVersion}({targetRID})"))
                        AnsiConsole.WriteLine()
                        trace.AppendLineToLogger $"Install .NET SDK {configuration.DotNet.SDKVersion} to {configuration.DotNet.DotNetRoot}"
                        let! dotNetRootInfo = DotNet.InstallDotNetSDKByVersion targetRID
                                                                               configuration.DotNet.SDKVersion 
                                                                               configuration.DotNet.DotNetRoot

                        // Install dotnet-dump
                        AnsiConsole.Write(new Rule($"Install dotnet-dump({configuration.DotNet.SDKVersion} - {targetRID})"))
                        AnsiConsole.WriteLine()
                        let toolName = "dotnet-dump"
                        trace.AppendLineToLogger $"Install {toolName} {configuration.DotNetDump.ToolVersion} to {configuration.DotNetDump.ToolRoot}"
                        yield! DotNetTool.InstallDotNetTool configuration.SystemInfo.EnvironmentVariables
                                                            configuration.DotNetDump.ToolRoot
                                                            configuration.DotNetDump.Feed 
                                                            ""
                                                            configuration.DotNetDump.ToolVersion 
                                                            toolName

                    } |> ignore
                    
                    // Test Cross OS DAC
                    //AnsiConsole.Write(new Rule($"Test Cross OS DAC for {configuration.DotNet.SDKVersion}({targetRID})"))
                    //AnsiConsole.WriteLine()
                    //CrossOSDAC.TestCrossOSDAC configuration targetRID
                    )