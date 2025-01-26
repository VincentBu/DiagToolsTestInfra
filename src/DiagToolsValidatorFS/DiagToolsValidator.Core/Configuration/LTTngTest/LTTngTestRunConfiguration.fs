namespace DiagToolsValidator.Core.Configuration

open System
open System.IO
open System.Collections.Generic
open System.Collections

open YamlDotNet.Serialization

open DiagToolsValidator.Core.Functionality

module LTTngTestRunConfiguration =
    type SystemInformation() =
        member val EnvironmentVariables = new Dictionary<string, string>() with get, set

    type DotNetSetting() =
        member val SDKVersion: string = null with get, set
        member val DotNetRoot: string = null with get, set

    type TargetAppSetting() =
        member val BuildConfig: string = null with get, set
        member val GCPerfsim: DotNetApp.DotNetApp = new DotNetApp.DotNetApp() with get, set
        
    type LTTngTestBaseConfiguration() =
        member val TestBed: string = null with get, set
        member val SDKVersionList: string list = List.empty with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set
   
    type LTTngTestRunConfiguration() =
        member val TestBed: string = null with get, set
        member val TestResultFolder: string = null with get, set
        member val PerfCollectPath: string = null with get, set
        member val DotNet: DotNetSetting = new DotNetSetting() with get, set
        member val TargetApp: TargetAppSetting = new TargetAppSetting() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set

    type LTTngTestConfiguration() =
        member val TestBed: string = null with get, set
        member val RunConfigurationList: LTTngTestRunConfiguration list = List.empty with get, set

    [<AbstractClass;Sealed>]
    type LTTngTestConfigurationGenerator private() =
        static let ParseConfigFile(path: string) = 
            let _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build()
            let serializedConfiguration = File.ReadAllText(path)

            try
                let configuration = _deserializer.Deserialize<LTTngTestBaseConfiguration>(serializedConfiguration);
                let parseExn = new exn("Fail to parse config file")
                if List.isEmpty configuration.SDKVersionList
                then
                    parseExn.Data.Add(nameof(LTTngTestConfigurationGenerator), "Please specify .NET SDK version")
                    raise(parseExn)
                
                elif String.IsNullOrEmpty configuration.TestBed
                then
                    parseExn.Data.Add(nameof(LTTngTestConfigurationGenerator), "Please specify testbed")
                    raise(parseExn)

                else 
                    configuration
            with ex -> 
                reraise()
         
        static member GenerateRunConfiguration(path: string) = 
            let baseConfig = ParseConfigFile path
            let aggregatedConfig = new LTTngTestConfiguration()
            aggregatedConfig.TestBed <- baseConfig.TestBed
            aggregatedConfig.RunConfigurationList <-
                baseConfig.SDKVersionList
                |> List.map(
                    fun sdkVersion ->
                        let configuration = new LTTngTestRunConfiguration()

                        configuration.TestBed <- baseConfig.TestBed
                        configuration.TestResultFolder <- Path.Combine(configuration.TestBed, "TestResult")

                        configuration.DotNet.SDKVersion <- sdkVersion
                        configuration.DotNet.DotNetRoot <- Path.Combine(configuration.TestBed, $"dotnet-sdk-{sdkVersion}")

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

                        let targetAppsRoot = Path.Combine(configuration.TestBed, "TargetApps")
                        configuration.TargetApp.BuildConfig <- baseConfig.TargetApp.BuildConfig
                        configuration.TargetApp.GCPerfsim <- new DotNetApp.DotNetApp(configuration.SystemInfo.EnvironmentVariables,
                                                                                    "console",
                                                                                    Path.Combine(targetAppsRoot, $"gcperfsim-sdk{sdkVersion}-{DotNet.CurrentRID}"))
                        configuration.PerfCollectPath <- Path.Combine(configuration.TestBed, "perfcollect")
                        configuration)

            aggregatedConfig
             