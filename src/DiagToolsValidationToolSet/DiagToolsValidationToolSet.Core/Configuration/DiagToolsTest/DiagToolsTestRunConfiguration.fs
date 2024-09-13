namespace DiagToolsValidationToolSet.Core.Configuration

open System.IO

open YamlDotNet.Serialization

open DiagToolsValidationToolSet.Core.Utility
open System.Collections.Generic

module DiagToolsTestRun =
    type SystemInformation() =
        member val OSName: string = null with get, set
        member val CPUArchitecture: string = null with get, set
        
    type DiagToolTestParameter() =
        member val DiagToolVersion: string = null with get, set
        member val ToolsToTest: string list = List.empty with get, set 
        member val Feed: string = null with get, set

    type DiagToolsTestRunBaseConfiguration() =
        member val SDKVersion: string = null with get, set
        member val TestBed: string = null with get, set
        member val OptionalFeatureContainer: bool = true with get, set
        member val DiagTool: DiagToolTestParameter = new DiagToolTestParameter() with get, set
        member val SystemInfo: SystemInformation = new SystemInformation() with get, set

    type DiagToolsTestRunConfiguration() =
        inherit DiagToolsTestRunBaseConfiguration()
        member val TestResultFolder: string = null with get, set
        member val DotnetRoot: string = null with get, set
        member val DotnetBinPath: string = null with get, set
        member val DiagToolRoot: string = null with get, set
        member val EnvironmentVariable: Dictionary<string, string> = new Dictionary<string, string>() with get, set

    [<AbstractClass;Sealed>]
    type DiagToolsTestRunConfigurationGenerator private() =

        static member ParseConfigFile(path: string) = 
            let _deserializer = (new DeserializerBuilder()).Build()
            let serializedConfiguration = File.ReadAllText(path)

            try
                let configuration = _deserializer.Deserialize<DiagToolsTestRunBaseConfiguration>(serializedConfiguration);

                if Common.IsNullOrEmptyString configuration.SDKVersion
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify .NET SDK version."))
                
                elif Common.IsNullOrEmptyString configuration.TestBed
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify testbed."))
                
                elif Common.IsNullOrEmptyString configuration.DiagTool.DiagToolVersion
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify diag tool version."))
                
                elif Common.IsNullOrEmptyString configuration.DiagTool.Feed
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify diag tool feed."))

                elif Common.IsNullOrEmptyString configuration.SystemInfo.OSName
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify os name."))
                
                elif Common.IsNullOrEmptyString configuration.SystemInfo.CPUArchitecture
                then Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: Please specify processor architecture."))

                else Choice1Of2 configuration
            with ex -> Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: {ex.Message}."))
         
        static member GenerateConfiguration(baseConfig: DiagToolsTestRunBaseConfiguration) =

            try
                let config = new DiagToolsTestRunConfiguration()
                config.SDKVersion <- baseConfig.SDKVersion
                config.TestBed <- baseConfig.TestBed
                config.OptionalFeatureContainer <- baseConfig.OptionalFeatureContainer
                config.DiagTool <- baseConfig.DiagTool
                config.SystemInfo <- baseConfig.SystemInfo

                let optionalFeatureContainerMark =
                    match config.OptionalFeatureContainer with
                    | true -> ""
                    | false -> "-NO"
                let testName = $"DiagTools-{config.SystemInfo.OSName}-{config.SystemInfo.CPUArchitecture}-SDK{config.SDKVersion}-Tool{config.DiagTool.DiagToolVersion}{optionalFeatureContainerMark}"
            
                config.TestBed <- Path.Combine(config.TestBed, $"Testbed-{testName}")
                config.TestResultFolder <- Path.Combine(config.TestBed, $"TestResult-{testName}")
                config.DotnetRoot <- Path.Combine(config.TestBed, $"dotnet-sdk")
                config.DotnetBinPath <- Path.Combine(config.DotnetRoot, $"dotnet")
                config.DiagToolRoot <- Path.Combine(config.TestBed, $"diag-tool")
                config.EnvironmentVariable["DOTNET_ROOT"] <- Path.Combine(config.TestBed, $"dotnet-sdk")

                Choice1Of2 config
            
            with ex -> Choice2Of2 (new exn($"{nameof(DiagToolsTestRunConfigurationGenerator)}: {ex.Message}."))