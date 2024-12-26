namespace DiagToolsValidator.Core.Configuration

open System
open System.IO
open System.Text
open System.Collections
open System.Collections.Generic
open System.Runtime.InteropServices

open YamlDotNet.Serialization

open DiagToolsValidator.Core.CoreFunctionality


module DiagToolsTestConfiguration =
    type SystemInformation() =
        member val OSName: string = null with get, set
        member val CPUArchitecture: string = null with get, set
        member val EnvironmentVariables = new Dictionary<string, string>() with get, set
        member val CLIDebugger: string = null with get, set
        
    type DotNetSetting() =
        member val SDKVersion: string = null with get, set
        member val DotNetRoot: string = null with get, set

    type DiagToolSetting() =
        member val DiagToolVersion: string = null with get, set
        member val ToolsToTest: string list = List.empty with get, set 
        member val ToolRoot: string = null with get, set
        member val Feed: string = null with get, set

    type TargetAppSetting() =
        member val BuildConfig: string = null with get, set
        member val ConsoleApp: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set
        member val WebApp: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set
        member val GCDumpPlayground: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set

    type DiagToolsTestConfiguration() =
        member val TestName: string = null with get, set
        member val TestBed: string = null with get, set
        member val TestResultFolder: string = null with get, set
        member val OptionalFeaturedContainer: bool = true with get, set
        member val DotNet: DotNetSetting = new DotNetSetting() with get, set
        member val DiagTool: DiagToolSetting = new DiagToolSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set

    [<AbstractClass;Sealed>]
    type DiagToolsTestConfigurationGenerator private() =

        static member GenerateConfiguration(path: string) = 
            let _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build()
            let serializedConfiguration = File.ReadAllText(path)

            try
                let configuration = _deserializer.Deserialize<DiagToolsTestConfiguration>(serializedConfiguration);
                let parseExn = new exn()
                if String.IsNullOrEmpty configuration.DotNet.SDKVersion
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify .NET SDK version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.TestBed
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify testbed")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DiagTool.DiagToolVersion
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify diag tool version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DiagTool.Feed
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify diag tool feed")
                    raise(parseExn)

                elif String.IsNullOrEmpty configuration.SystemInfo.OSName
                then 
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify os name")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.SystemInfo.CPUArchitecture
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify processor architecture")
                    raise(parseExn)

                elif String.IsNullOrEmpty configuration.SystemInfo.CLIDebugger
                then
                    parseExn.Data.Add(nameof(DiagToolsTestConfigurationGenerator), "Please specify debugger")
                    raise(parseExn)

                else 
                    if String.IsNullOrEmpty(configuration.TestName)
                    then 
                        let testName = new StringBuilder("DiagTools")
                        testName.Append($"-{configuration.SystemInfo.OSName}") |> ignore
                        testName.Append($"-{configuration.SystemInfo.CPUArchitecture}") |> ignore
                        testName.Append($"-SDK{configuration.DotNet.SDKVersion}") |> ignore
                        testName.Append($"-Tool{configuration.DiagTool.DiagToolVersion}") |> ignore
                        let optionalFeaturedContainerFlag = 
                            match configuration.OptionalFeaturedContainer with
                            | true -> ""
                            | false -> "-NO"
                        testName.Append(optionalFeaturedContainerFlag) |> ignore
                        configuration.TestName <- testName.ToString()
                    configuration.TestResultFolder <- Path.Combine(configuration.TestBed, $"TestResult-{configuration.TestName}")

                    configuration.DotNet.DotNetRoot <- Path.Combine(configuration.TestBed, "dotnet-sdk")

                    configuration.DiagTool.ToolRoot <- Path.Combine(configuration.TestBed, "diag-tools")
                    
                    configuration.SystemInfo.EnvironmentVariables["DOTNET_ROOT"] <- configuration.DotNet.DotNetRoot
                    for de: DictionaryEntry in Environment.GetEnvironmentVariables() |> Seq.cast<DictionaryEntry> do
                        configuration.SystemInfo.EnvironmentVariables[de.Key.ToString()] <- de.Value.ToString()
                    if RuntimeInformation.RuntimeIdentifier.Contains("win")
                    then 
                        configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                            Environment.GetEnvironmentVariable("Path") + $";{configuration.DotNet.DotNetRoot}"
                    else
                        configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                            Environment.GetEnvironmentVariable("Path") + $":{configuration.DotNet.DotNetRoot}"

                    let targetAppsRoot = Path.Combine(configuration.TestBed, "TargetApps")
                    configuration.TargetApp.ConsoleApp <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                  "console",
                                                                                  Path.Combine(targetAppsRoot, "console"))
                    configuration.TargetApp.WebApp <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                              "webapp",
                                                                              Path.Combine(targetAppsRoot, "webapp"))
                    configuration.TargetApp.GCDumpPlayground <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                        "console",
                                                                                        Path.Combine(targetAppsRoot, "GCDumpPlayground"))
                

                configuration
            with _ -> 
                reraise()
         
            