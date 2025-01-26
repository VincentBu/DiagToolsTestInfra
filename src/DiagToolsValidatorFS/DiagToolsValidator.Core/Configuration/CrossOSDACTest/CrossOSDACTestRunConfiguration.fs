namespace DiagToolsValidator.Core.Configuration

open System
open System.IO
open System.Collections.Generic
open System.Collections

open YamlDotNet.Serialization

open DiagToolsValidator.Core.Functionality

module CrossOSDACTestRunConfiguration =
    type SystemInformation() =
        member val EnvironmentVariables = new Dictionary<string, string>() with get, set

    type DotNetSetting() =
        member val SDKVersion: string = null with get, set
        member val DotNetRoot: string = null with get, set

    type DotNetDumpSetting() =
        member val ToolVersion: string = null with get, set
        member val ToolRoot: string = null with get, set
        member val Feed: string = null with get, set

    type TargetAppSetting() =
        member val BuildConfig: string = null with get, set
        member val OOMApp: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set
        member val UHEApp: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set

    type CrossOSDACTestBaseConfiguration() =
        member val TestBed: string = null with get, set
        member val SDKVersionList: string list = List.empty with get, set
        member val DotNetDump: DotNetDumpSetting = new DotNetDumpSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set
   
    
    type CrossOSDACTestRunConfiguration() =
        member val TestBed: string = null with get, set
        member val TestAssets: string = null with get, set
        member val DumpFolder: string = null with get, set
        member val AnalysisOutputFolder: string = null with get, set
        member val DotNet: DotNetSetting = new DotNetSetting() with get, set
        member val DotNetDump: DotNetDumpSetting = new DotNetDumpSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set

    type CrossOSDACTestConfiguration() =
        member val TestBed: string = null with get, set
        member val RunConfigurationList: CrossOSDACTestRunConfiguration list = List.empty with get, set

    [<AbstractClass;Sealed>]
    type CrossOSDACTestConfigurationGenerator private() =
        static let ParseConfigFile(path: string) = 
            let _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build()
            let serializedConfiguration = File.ReadAllText(path)

            try
                let configuration = _deserializer.Deserialize<CrossOSDACTestBaseConfiguration>(serializedConfiguration);
                let parseExn = new exn("Fail to parse config file")
                if List.isEmpty configuration.SDKVersionList
                then
                    parseExn.Data.Add(nameof(CrossOSDACTestConfigurationGenerator), "Please specify .NET SDK version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.TestBed
                then
                    parseExn.Data.Add(nameof(CrossOSDACTestConfigurationGenerator), "Please specify testbed")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DotNetDump.ToolVersion
                then
                    parseExn.Data.Add(nameof(CrossOSDACTestConfigurationGenerator), "Please specify dotnet-dump version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.DotNetDump.Feed
                then
                    parseExn.Data.Add(nameof(CrossOSDACTestConfigurationGenerator), "Please specify dotnet-dump feed")
                    raise(parseExn)

                else 
                    configuration
            with ex -> 
                reraise()
         
        static member GenerateRunConfiguration(path: string) = 
            let baseConfig = ParseConfigFile path
            let aggregatedConfig = new CrossOSDACTestConfiguration()
            aggregatedConfig.TestBed <- baseConfig.TestBed
            aggregatedConfig.RunConfigurationList <-
                baseConfig.SDKVersionList
                |> List.map(
                    fun sdkVersion ->
                        let configuration = new CrossOSDACTestRunConfiguration()

                        configuration.TestBed <- baseConfig.TestBed
                        configuration.TestAssets <- Path.Combine(configuration.TestBed, "TestAssets")

                        configuration.DotNet.SDKVersion <- sdkVersion
                        configuration.DotNet.DotNetRoot <- Path.Combine(configuration.TestBed, $"dotnet-sdk-{sdkVersion}")

                        configuration.DumpFolder <- Path.Combine(configuration.TestAssets, "dump", configuration.DotNet.SDKVersion)
                        configuration.AnalysisOutputFolder <- Path.Combine(configuration.TestAssets, "analysisoutput", configuration.DotNet.SDKVersion)
                        
                        configuration.DotNetDump.ToolVersion <- baseConfig.DotNetDump.ToolVersion
                        configuration.DotNetDump.Feed <- baseConfig.DotNetDump.Feed
                        configuration.DotNetDump.ToolRoot <- Path.Combine(configuration.TestBed, $"dotnet-dump-sdk{sdkVersion}")
                    
                        for de: DictionaryEntry in Environment.GetEnvironmentVariables() |> Seq.cast<DictionaryEntry> do
                            configuration.SystemInfo.EnvironmentVariables[de.Key.ToString()] <- de.Value.ToString()
                        if DotNet.CurrentRID.Contains("win")
                        then 
                            configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                                Environment.GetEnvironmentVariable("Path") + $";{configuration.DotNet.DotNetRoot}"
                        else
                            configuration.SystemInfo.EnvironmentVariables["Path"] <- 
                                Environment.GetEnvironmentVariable("Path") + $":{configuration.DotNet.DotNetRoot}"
                        configuration.SystemInfo.EnvironmentVariables["DOTNET_ROOT"] <- configuration.DotNet.DotNetRoot

                        let targetAppsRoot = Path.Combine(configuration.TestAssets, "TargetApps", configuration.DotNet.SDKVersion)
                        configuration.TargetApp.BuildConfig <- baseConfig.TargetApp.BuildConfig
                        configuration.TargetApp.OOMApp <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                  "console",
                                                                                  Path.Combine(targetAppsRoot, $"oom-sdk{sdkVersion}-{DotNet.CurrentRID}"))
                        configuration.TargetApp.UHEApp <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                  "console",
                                                                                  Path.Combine(targetAppsRoot, $"uhe-sdk{sdkVersion}-{DotNet.CurrentRID}"))
                        
                        configuration)

            aggregatedConfig
            