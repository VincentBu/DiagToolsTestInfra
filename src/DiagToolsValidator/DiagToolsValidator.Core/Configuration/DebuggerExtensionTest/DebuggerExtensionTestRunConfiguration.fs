namespace DiagToolsValidator.Core.Configuration

open System
open System.IO
open System.Text
open System.Collections.Generic
open System.Collections
open System.Runtime.InteropServices

open YamlDotNet.Serialization

open DiagToolsValidator.Core.Functionality


module DebuggerExtensionTestConfiguration =
    type SystemInformation() =
        member val EnvironmentVariables = new Dictionary<string, string>() with get, set
        member val CLIDebugger: string = null with get, set

    type DotNetSetting() =
        member val SDKVersion: string = null with get, set
        member val DotNetRoot: string = null with get, set

    type DebuggerExtensionSetting() =
        member val ToolVersion: string = null with get, set
        member val ToolRoot: string = null with get, set
        member val Feed: string = null with get, set
        member val UserName: string = null with get, set
        member val Token: string = null with get, set

    type TargetAppSetting() =
        member val BuildConfig: string = null with get, set
        member val NativeAOTApp: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set

    type DebuggerExtensionTestBaseConfiguration() =
        member val TestBed: string = null with get, set
        member val SDKVersionList: string list = List.empty with get, set
        member val DebuggerExtension: DebuggerExtensionSetting = new DebuggerExtensionSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set
      
    type DebuggerExtensionTestRunConfiguration() =
        member val TestName: string = null with get, set
        member val TestBed: string = null with get, set
        member val TestResultFolder: string = null with get, set
        member val DotNet: DotNetSetting = new DotNetSetting() with get, set
        member val DebuggerExtension: DebuggerExtensionSetting = new DebuggerExtensionSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set
        
    type DebuggerExtensionTestConfiguration() =
        member val TestBed: string = null with get, set
        member val RunConfigurationList: DebuggerExtensionTestRunConfiguration list = List.empty with get, set

    [<AbstractClass;Sealed>]
    type DebuggerExtensionTestConfigurationGenerator private() =
        static let ParseConfigFile(path: string) = 
            let _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build()
            let serializedConfiguration = File.ReadAllText(path)

            try
                let configuration = _deserializer.Deserialize<DebuggerExtensionTestBaseConfiguration>(serializedConfiguration);
                let parseExn = new exn("Fail to parse config file")
                if List.isEmpty configuration.SDKVersionList
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify .NET SDK version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.TestBed
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify testbed")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DebuggerExtension.ToolVersion
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify debugger extension version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DebuggerExtension.Feed
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify debugger extension feed")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DebuggerExtension.Token
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify debugger extension token")
                    raise(parseExn)

                elif String.IsNullOrEmpty configuration.SystemInfo.CLIDebugger
                then
                    parseExn.Data.Add(nameof(DebuggerExtensionTestConfigurationGenerator), "Please specify debugger")
                    raise(parseExn)

                else 
                    configuration
            with ex -> 
                reraise()
         
        static member GenerateRunConfiguration(path: string) = 
            let baseConfig = ParseConfigFile path
            let aggregatedConfig = new DebuggerExtensionTestConfiguration()
            aggregatedConfig.TestBed <- baseConfig.TestBed
            aggregatedConfig.RunConfigurationList <-
                baseConfig.SDKVersionList
                |> List.map(
                    fun sdkVersion ->
                        let configuration = new DebuggerExtensionTestRunConfiguration()
                        // Generate testing configuration list
                        if String.IsNullOrEmpty(configuration.TestName)
                        then 
                            let testName = new StringBuilder("DiagTools")
                            testName.Append($"-{DotNet.CurrentRID}") |> ignore
                            testName.Append($"-SDK{sdkVersion}") |> ignore
                            testName.Append($"-Tool{configuration.DebuggerExtension.ToolVersion}") |> ignore
                            configuration.TestName <- testName.ToString()

                        configuration.TestBed <- baseConfig.TestBed
                        configuration.TestResultFolder <- Path.Combine(configuration.TestBed, $"TestResult-{configuration.TestName}")
                        
                        configuration.DotNet.SDKVersion <- sdkVersion
                        configuration.DotNet.DotNetRoot <- Path.Combine(configuration.TestBed, $"dotnet-sdk-{sdkVersion}")
                        
                        configuration.DebuggerExtension.ToolVersion <- baseConfig.DebuggerExtension.ToolVersion
                        configuration.DebuggerExtension.Feed <- baseConfig.DebuggerExtension.Feed
                        configuration.DebuggerExtension.UserName <- baseConfig.DebuggerExtension.UserName
                        configuration.DebuggerExtension.Token <- baseConfig.DebuggerExtension.Token
                        configuration.DebuggerExtension.ToolRoot <- Path.Combine(configuration.TestBed, $"debugger-extension-sdk{sdkVersion}")
                    
                        configuration.SystemInfo.EnvironmentVariables["DOTNET_ROOT"] <- configuration.DotNet.DotNetRoot
                        for de: DictionaryEntry in Environment.GetEnvironmentVariables() |> Seq.cast<DictionaryEntry> do
                            configuration.SystemInfo.EnvironmentVariables[de.Key.ToString()] <- de.Value.ToString()
                        if DotNet.CurrentRID.Contains("win")
                        then 
                            configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                                Environment.GetEnvironmentVariable("Path") + $";{configuration.DotNet.DotNetRoot}"
                        else
                            configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                                Environment.GetEnvironmentVariable("Path") + $":{configuration.DotNet.DotNetRoot}"

                        let targetAppsRoot = Path.Combine(configuration.TestBed, "TargetApps")
                        configuration.SystemInfo.CLIDebugger <- baseConfig.SystemInfo.CLIDebugger

                        configuration.TargetApp.BuildConfig <- baseConfig.TargetApp.BuildConfig
                        configuration.TargetApp.NativeAOTApp <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                        "console",
                                                                                        Path.Combine(targetAppsRoot, $"nativeaot-sdk{sdkVersion}"))
                        
                        configuration)

            aggregatedConfig
            