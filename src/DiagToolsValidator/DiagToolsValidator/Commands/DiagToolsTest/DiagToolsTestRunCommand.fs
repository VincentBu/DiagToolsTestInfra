namespace DiagToolsValidationToolSet.Command

open System.IO
open System.Runtime.InteropServices

open Spectre.Console
open Spectre.Console.Cli

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.CoreFunctionality
open DiagToolsValidator.Core.Task.DiagToolsTest

module DiagToolsTestRun =
    let RID = RuntimeInformation.RuntimeIdentifier


    type DiagToolsTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set


    type DiagToolsTestRunCommand() =
        inherit Command<DiagToolsTestRunSettings>()
        let _baseConsoleAppSrcPath = Path.Combine("Commands", "DiagToolsTest", "TargetApps", "consoleapp", "Program.cs.txt")
        let _baseGCDumpPlaygroundSrcPath = Path.Combine("Commands", "DiagToolsTest", "TargetApps", "GCDumpPlayground2", "Program.cs.txt")
        
        let TestRunnerMap = 
            [
                ("dotnet-counters", DotNetCounters.TestDotNetCounters);
                ("dotnet-dump", DotNetDump.TestDotNetDump);
                ("dotnet-gcdump", DotNetGCDump.TestDotNetGCDump);
                ("dotnet-sos", DotNetSOS.TestDotNetSOS);
                ("dotnet-stack", DotNetStack.TestDotNetStack);
                ("dotnet-trace", DotNetTrace.TestDotNetTrace);
            ]
            |> Map.ofList

        override this.Execute(context: CommandContext, setting: DiagToolsTestRunSettings) =
            AnsiConsole.Write((new FigletText("Diagnostic Tools Test")).Centered().Color(Color.Red))

            let configuration = DiagToolsTestConfiguration.DiagToolsTestConfigurationParser.GenerateConfigFile setting.ConfigurationPath
            
            // Create testbed and test result folder
            Core.CreateDirectory configuration.TestResultFolder |> ignore

            // Generate environment activation script
            (TestInfrastructure.GenerateEnvironmentActivationScript configuration) |> ignore

            let loggerPath = Path.Combine(configuration.TestResultFolder, "Initialization.txt")
            let trace = new Core.ProgressTraceBuilder(loggerPath)
            
            trace {
                
                // Install .NET SDK
                AnsiConsole.Write(new Rule("Install .NET SDK"))
                AnsiConsole.WriteLine()
                trace.AppendLineToLogger $"Install .NET SDK {configuration.DotNet.SDKVersion} to {configuration.DotNet.DotNetRoot}"
                let! dotNetRootInfo = DotNet.InstallDotNetSDKByVersion DotNet.CurrentRID 
                                                                       configuration.DotNet.SDKVersion 
                                                                       configuration.DotNet.DotNetRoot

                // Install diagnostic tools
                AnsiConsole.Write(new Rule("Install diagnostic tools"))
                AnsiConsole.WriteLine()
                for toolName in configuration.DiagTool.ToolsToTest do
                    trace.AppendLineToLogger $"Install {toolName} {configuration.DiagTool.DiagToolVersion} to {configuration.DiagTool.ToolRoot}"
                    yield! DotNetTool.InstallDotNetTool configuration.SystemInfo.EnvironmentVariables
                                                        configuration.DiagTool.ToolRoot
                                                        configuration.DiagTool.Feed 
                                                        ""
                                                        configuration.DiagTool.DiagToolVersion 
                                                        toolName

                let buildConfig = configuration.TargetApp.BuildConfig
                AnsiConsole.Write(new Rule("Prepare .NET apps for testing"))
                AnsiConsole.WriteLine()
                // Create console app
                trace.AppendLineToLogger  "Create Console App"
                yield! configuration.TargetApp.ConsoleApp.CreateApp()
            
                // Replace source file
                let targetConsoleAppSrcPath = Path.Combine(configuration.TargetApp.ConsoleApp.AppRoot, "Program.cs")
                let consoleSrcContent = File.ReadAllText(_baseConsoleAppSrcPath)
                File.WriteAllText(targetConsoleAppSrcPath, consoleSrcContent)
                
                // Build console app
                trace.AppendLineToLogger "Build Console App"
                yield! configuration.TargetApp.ConsoleApp.BuildApp(buildConfig)

                // Create GCDumpPlayground
                trace.AppendLineToLogger "Create GCDumpPlayground"
                yield! configuration.TargetApp.GCDumpPlayground.CreateApp()
            
                // Replace source file
                let targetGCDumpPlaygroundSrcPath = Path.Combine(configuration.TargetApp.GCDumpPlayground.AppRoot, "Program.cs")
                let gcDumpPlaygroundSrcContent = File.ReadAllText(_baseGCDumpPlaygroundSrcPath)
                File.WriteAllText(targetGCDumpPlaygroundSrcPath, gcDumpPlaygroundSrcContent)
                
                // Build GCDumpPlayground
                trace.AppendLineToLogger  "Build GCDumpPlayground App"
                yield! configuration.TargetApp.GCDumpPlayground.BuildApp(buildConfig)
            
                // Create webapp
                trace.AppendLineToLogger  "Create Webapp"
                yield! configuration.TargetApp.WebApp.CreateApp()
            
                // Build webapp
                trace.AppendLineToLogger "Build Webapp"
                yield! configuration.TargetApp.WebApp.BuildApp(buildConfig)
            } |> ignore
            
            // Start test
            configuration.DiagTool.ToolsToTest
            |> List.map (
                fun toolName ->
                    AnsiConsole.Write(new Rule($"Test {toolName}"))
                    AnsiConsole.WriteLine()
                    let testRunner = TestRunnerMap[toolName]
                    testRunner(configuration))
            |> ignore
            0
            
            