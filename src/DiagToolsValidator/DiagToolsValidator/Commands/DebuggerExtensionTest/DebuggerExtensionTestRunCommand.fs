namespace DiagToolsValidator.Command

open System.IO
open System.Xml.Linq

open Spectre.Console
open Spectre.Console.Cli

open DiagToolsValidator.Core.Configuration
open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Task.DebuggerExtension

module DebuggerExtensionTestRun =
    type DebuggerExtensionTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set

    type DebuggerExtensionTestRunCommand() =
        inherit Command<DebuggerExtensionTestRunSettings>()

        let _baseNativeAOTAppSrcPath = Path.Combine("Commands", "DebuggerExtensionTest", "TargetApps", "nativeaot", "Program.cs.txt")

        override this.Execute(context: CommandContext, setting: DebuggerExtensionTestRunSettings) =
            AnsiConsole.Write((new FigletText("Debugger Extension Test")).Centered().Color(Color.Red))

            let aggregatedConfig = 
                DebuggerExtensionTestConfiguration.DebuggerExtensionTestConfigurationGenerator.GenerateRunConfiguration setting.ConfigurationPath
            
            aggregatedConfig.RunConfigurationList
            |> List.map (
                fun configuration ->
                    // Create testbed and test result folder
                    Core.CreateDirectory configuration.TestResultFolder |> ignore
                    
                    let loggerPath = Path.Combine(configuration.TestResultFolder, "Initialization.txt")
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


                        // Prepare nativeaot app for testing (create, modify source code and project file, publish)
                        AnsiConsole.Write(new Rule($"Prepare .NET apps for testing({configuration.DotNet.SDKVersion})"))
                        AnsiConsole.WriteLine()
                        trace.AppendLineToLogger $"Create nativeaot app"
                        yield! configuration.TargetApp.NativeAOTApp.CreateApp()
                        
                        // Generate Nuget config file
                        let! nugetConfig = TestInfrastructure.GenerateNugetConfig configuration
                        trace.AppendLineToLogger $"Generate nuget config to {nugetConfig}"

                        // Modify project file
                        let! projectFile = configuration.TargetApp.NativeAOTApp.GetProjectFile
                        let xmlData = File.ReadAllText(projectFile)
                        let doc = XDocument.Parse(xmlData)
                        let propertyGroup = doc.Root.Element("PropertyGroup")
                        propertyGroup.Add(new XElement("AllowUnsafeBlocks", "true"))
                        propertyGroup.Add(new XElement("PublishAot", "true"))
                        doc.Save(projectFile)

                        // Replace source file
                        let targetNativeAOTAppSrcPath = Path.Combine(configuration.TargetApp.NativeAOTApp.AppRoot, "Program.cs")
                        let consoleSrcContent = File.ReadAllText(_baseNativeAOTAppSrcPath)
                        File.WriteAllText(targetNativeAOTAppSrcPath, consoleSrcContent)

                        trace.AppendLineToLogger $"Publish nativeaot app"
                        yield! configuration.TargetApp.NativeAOTApp.PublishApp configuration.TargetApp.BuildConfig DotNet.CurrentRID
                        
                        // Install dotnet-debugger-extensions
                        AnsiConsole.Write(new Rule($"Install dotnet-debugger-extensions({configuration.DotNet.SDKVersion})"))
                        AnsiConsole.WriteLine()
                        let toolName = "dotnet-debugger-extensions"
                        trace.AppendLineToLogger $"Install {toolName} {configuration.DebuggerExtension.ToolVersion} to {configuration.DebuggerExtension.ToolRoot}"
                        yield! DotNetTool.InstallDotNetTool configuration.SystemInfo.EnvironmentVariables
                                                            configuration.DebuggerExtension.ToolRoot
                                                            configuration.DebuggerExtension.Feed 
                                                            nugetConfig
                                                            configuration.DebuggerExtension.ToolVersion 
                                                            toolName

                    } |> ignore
                    
                    // Test dotnet-debugger-extensions
                    AnsiConsole.Write(new Rule($"Test dotnet-debugger-extensions"))
                    AnsiConsole.WriteLine()
                    DebuggerExtension.TestDebuggerExtension configuration
                )
            |> ignore
            0