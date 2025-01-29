using System.Collections;

using DiagToolsValidationRunner.Core.Functionality;

using YamlDotNet.Serialization;

namespace DiagToolsValidationRunner.Core.Configuration.CrossOSDACTest
{
    public class TargetAppSetting : BaseTargetAppSetting
    {
        public DotNetApp? OOM;
        public DotNetApp? UHE;
    }

    public class TestSetting : BaseTestSetting
    {
        public string? DumpFolder;
        public string? AnalysisOutputFolder;
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
        public required List<CrossOSDACTestRunConfiguration> CrossOSDACTestRunConfigurationList;
    }

    public static class CrossOSDACTestConfigurationGenerator
    {
        private static IDeserializer _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();

        private static CrossOSDACTestConfiguration ParseConfigFile(string configFile)
        {
            try
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
            catch
            {
                throw;
            }
        }

        public static CrossOSDACTestConfiguration GenerateConfiguration(string configFile)
        {
            CrossOSDACTestConfiguration configuration = ParseConfigFile(configFile);
            string testResultFolder = Path.Combine(configuration.Test.TestBed, "TestResult");

            configuration.CrossOSDACTestRunConfigurationList = new();
            foreach (string SDKVersion in configuration.SDKVersionList)
            {
                string testName = $"CrossOSDAC-SDK{SDKVersion}";
                string dumpFolder = Path.Combine(testResultFolder, $"dumps-sdk{SDKVersion}");
                string analysisOutputFolder = Path.Combine(testResultFolder, $"analysis-sdk{SDKVersion}");

                string dotNetRoot = Path.Combine(configuration.Test.TestBed, $"DotNetSDK-{SDKVersion}");

                string dotNetDumpVersion = configuration.DotNetDumpSetting.Version;
                string dotNetDumpFeed = configuration.DotNetDumpSetting.Feed;
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

                CrossOSDACTestRunConfiguration runConfig = new()
                {
                    Test = new()
                    {
                        TestBed = configuration.Test.TestBed,
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
                        BuildConfig = configuration.AppSetting.BuildConfig,
                        OOM = new(env, "console", oomAppRoot),
                        UHE = new(env, "console", uheAppRoot)
                    }
                };

                configuration.CrossOSDACTestRunConfigurationList.Add(runConfig);
            }

            return configuration;
        }
    }
}
