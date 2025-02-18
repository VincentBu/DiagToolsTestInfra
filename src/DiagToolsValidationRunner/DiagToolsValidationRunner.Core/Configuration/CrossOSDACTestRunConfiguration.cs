using System.Collections;

using DiagToolsValidationRunner.Core.Functionality;

using YamlDotNet.Serialization;

namespace DiagToolsValidationRunner.Core.Configuration.CrossOSDACTest
{
    public class TargetAppSetting : BaseTargetAppSetting
    {
        public DotNetApp OOM = new();
        public DotNetApp UHE = new();
    }

    public class TestSetting : BaseTestSetting
    {
        public string DumpFolder = String.Empty;
        public string AnalysisOutputFolder = String.Empty;
    }

    public class CrossOSDACTestRunConfiguration
    {
        public required TestSetting Test;
        public required DotNetSDKSetting SDKSetting;
        public required DotNetToolSetting DotNetDumpSetting;
        public required TargetAppSetting AppSetting;
        public required BaseSystemInformation SysInfo;
    }

    public class CrossOSDACTestConfiguration
    {
        public required BaseTestSetting Test;
        public required List<string> SDKVersionList;
        public required DotNetToolSetting DotNetDumpSetting;
        public required TargetAppSetting AppSetting;
        public List<CrossOSDACTestRunConfiguration> CrossOSDACTestRunConfigurationList = new();
    }

    public static class CrossOSDACTestConfigurationGenerator
    {
        private static IDeserializer _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();

        private static CrossOSDACTestConfiguration ParseConfigFile(string configFile)
        {
            string serializedConfiguration = File.ReadAllText(configFile);
            CrossOSDACTestConfiguration baseConfiguration =
                _deserializer.Deserialize<CrossOSDACTestConfiguration>(configFile);

            if (string.IsNullOrEmpty(baseConfiguration.Test.TestBed))
            {
                throw new Exception(
                    $"{nameof(CrossOSDACTestConfigurationGenerator)}: Please specify testbed");
            }

            else if (baseConfiguration.SDKVersionList.Count == 0)
            {
                throw new Exception(
                    $"{nameof(CrossOSDACTestConfigurationGenerator)}: Please specify at least one SDK Version");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DotNetDumpSetting.Version))
            {
                throw new Exception(
                    $"{nameof(CrossOSDACTestConfigurationGenerator)}: Please specify tool version");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DotNetDumpSetting.Feed))
            {
                throw new Exception(
                    $"{nameof(CrossOSDACTestConfigurationGenerator)}: Please specify tool install feed");
            }

            else if (!(new List<string> { "Debug", "Release" }.Contains(baseConfiguration.AppSetting.BuildConfig)))
            {
                throw new Exception(
                    $"{nameof(CrossOSDACTestConfigurationGenerator)}: Please specify valid build config");
            }

            return baseConfiguration;
        }

        private static CrossOSDACTestRunConfiguration CreateRunConfig(string testbed,
                                                                      string testResultFolder,
                                                                      string testName,
                                                                      string dumpFolder,
                                                                      string analysisOutputFolder,
                                                                      string dotNetDumpVersion,
                                                                      string dotNetDumpFeed,
                                                                      string dotNetDumpRoot,
                                                                      string SDKVersion,
                                                                      string dotNetRoot,
                                                                      Dictionary<string, string> env,
                                                                      string buildConfig,
                                                                      string oomAppRoot,
                                                                      string uheAppRoot)
        {
            return new()
            {
                Test = new()
                {
                    TestBed = testbed,
                    TestResultFolder = testResultFolder,
                    TestName = testName,
                    DumpFolder = dumpFolder,
                    AnalysisOutputFolder = analysisOutputFolder
                },
                DotNetDumpSetting = new()
                {
                    Version = dotNetDumpVersion,
                    Feed = dotNetDumpFeed,
                    ToolRoot = dotNetDumpRoot
                },
                SDKSetting = new()
                {
                    Version = SDKVersion,
                    DotNetRoot = dotNetRoot,
                },
                SysInfo = new()
                {
                    EnvironmentVariables = new(env)
                },
                AppSetting = new()
                {
                    BuildConfig = buildConfig,
                    OOM = new(env, "console", oomAppRoot),
                    UHE = new(env, "console", uheAppRoot)
                }
            };
        }

        public static CrossOSDACTestConfiguration GenerateConfiguration(string configFile)
        {
            CrossOSDACTestConfiguration configuration = ParseConfigFile(configFile);
            string testResultFolder = Path.Combine(configuration.Test.TestBed, "TestResult");


            configuration.CrossOSDACTestRunConfigurationList = new();
            foreach (string SDKVersion in configuration.SDKVersionList)
            {
                if (OperatingSystem.IsWindows())
                {
                    List<string> winRIDList = new() { "win-x86", "win-x64" };
                    foreach (var rid in winRIDList)
                    {
                        string testName = $"CrossOSDAC-SDK{SDKVersion}-{rid}";
                        string dumpFolder = Path.Combine(testResultFolder, $"dumps-sdk{SDKVersion}");
                        string analysisOutputFolder = Path.Combine(testResultFolder, $"analysis-sdk{SDKVersion}");

                        string dotNetRoot = Path.Combine(configuration.Test.TestBed, $"DotNetSDK-{SDKVersion}-{rid}");

                        string dotNetDumpRoot = Path.Combine(configuration.Test.TestBed, $"dotnet-dump-{SDKVersion}-{rid}");

                        Dictionary<string, string> env = new();
                        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                        {
                            env[de!.Key!.ToString()!] = de!.Value!.ToString()!;
                        }
                        env["DOTNET_ROOT"] = dotNetRoot;

                        string targetAppsRoot = Path.Combine(testResultFolder, "TargetApps");
                        string oomAppRoot = Path.Combine(targetAppsRoot, $"oom-sdk{SDKVersion}");
                        string uheAppRoot = Path.Combine(targetAppsRoot, $"uhe-sdk{SDKVersion}");

                        CrossOSDACTestRunConfiguration runConfig = CreateRunConfig(configuration.Test.TestBed,
                                                                                   testResultFolder,
                                                                                   testName,
                                                                                   dumpFolder,
                                                                                   analysisOutputFolder,
                                                                                   configuration.DotNetDumpSetting.Version,
                                                                                   configuration.DotNetDumpSetting.Feed,
                                                                                   dotNetDumpRoot,
                                                                                   SDKVersion,
                                                                                   dotNetRoot,
                                                                                   env,
                                                                                   configuration.AppSetting.BuildConfig,
                                                                                   oomAppRoot,
                                                                                   uheAppRoot);

                        configuration.CrossOSDACTestRunConfigurationList.Add(runConfig);
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    string testName = $"CrossOSDAC-SDK{SDKVersion}";
                    string dumpFolder = Path.Combine(testResultFolder, $"dumps-sdk{SDKVersion}");
                    string analysisOutputFolder = Path.Combine(testResultFolder, $"analysis-sdk{SDKVersion}");
                    string dotNetRoot = Path.Combine(configuration.Test.TestBed, $"DotNetSDK-{SDKVersion}");
                    string dotNetDumpRoot = Path.Combine(configuration.Test.TestBed, $"dotnet-dump-{SDKVersion}");

                    Dictionary<string, string> env = new();
                    foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                    {
                        env[de!.Key!.ToString()!] = de!.Value!.ToString()!;
                    }
                    env["DOTNET_ROOT"] = dotNetRoot;

                    string targetAppsRoot = Path.Combine(testResultFolder, "TargetApps");
                    string oomAppRoot = Path.Combine(targetAppsRoot, $"oom-sdk{SDKVersion}");
                    string uheAppRoot = Path.Combine(targetAppsRoot, $"uhe-sdk{SDKVersion}");

                    CrossOSDACTestRunConfiguration runConfig = CreateRunConfig(configuration.Test.TestBed,
                                                                               testResultFolder,
                                                                               testName,
                                                                               dumpFolder,
                                                                               analysisOutputFolder,
                                                                               configuration.DotNetDumpSetting.Version,
                                                                               configuration.DotNetDumpSetting.Feed,
                                                                               dotNetDumpRoot,
                                                                               SDKVersion,
                                                                               dotNetRoot,
                                                                               env,
                                                                               configuration.AppSetting.BuildConfig,
                                                                               oomAppRoot,
                                                                               uheAppRoot);

                    configuration.CrossOSDACTestRunConfigurationList.Add(runConfig);
                }
                else
                {
                    throw new Exception($"{nameof(CrossOSDACTestConfigurationGenerator)}: Unsupported OS");
                }
            }

            return configuration;
        }
    }
}
