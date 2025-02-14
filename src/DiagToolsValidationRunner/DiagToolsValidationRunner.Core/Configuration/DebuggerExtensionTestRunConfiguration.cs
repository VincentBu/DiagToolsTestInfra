using System.Collections;

using YamlDotNet.Serialization;

using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Core.Configuration.DebuggerExtensionTest
{
    public class TargetAppSetting : BaseTargetAppSetting
    {
        public DotNetApp NativeAOTApp = new();
    }

    public class TestSetting : BaseTestSetting
    {
        public string DumpFolder = String.Empty;
        public string AnalysisOutputFolder = String.Empty;
    }

    public class SystemInformation : BaseSystemInformation
    {
        public required string CLIDebugger;
    }

    public class DebuggerExtensionSetting: DotNetToolSetting
    {
        public required string UserName;
        public required string Token;
    }

    public class DebuggerExtensionTestRunConfiguration
    {
        public required TestSetting Test;
        public required DotNetSDKSetting SDKSetting;
        public required DebuggerExtensionSetting DebuggerExtensionSetting;
        public required TargetAppSetting AppSetting;
        public required SystemInformation SysInfo;

    }

    public class DebuggerExtensionTestConfiguration
    {
        public required TestSetting Test;
        public required List<string> SDKVersionList;
        public required DebuggerExtensionSetting DebuggerExtensionSetting;
        public required TargetAppSetting AppSetting;
        public required SystemInformation SysInfo;
        public List<DebuggerExtensionTestRunConfiguration> DebuggerExtensionTestRunConfigurationList = new();
    }

    public static class DebuggerExtensionTestConfigurationGenerator
    {
        private static IDeserializer _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();

        private static DebuggerExtensionTestConfiguration ParseConfigFile(string configFile)
        {
            string serializedConfiguration = File.ReadAllText(configFile);
            DebuggerExtensionTestConfiguration baseConfiguration =
                _deserializer.Deserialize<DebuggerExtensionTestConfiguration>(configFile);

            if (string.IsNullOrEmpty(baseConfiguration.Test.TestBed))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify testbed");
            }

            else if (baseConfiguration.SDKVersionList.Count == 0)
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify at least one SDK Version");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DebuggerExtensionSetting.Version))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify tool version");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DebuggerExtensionSetting.Feed))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify tool install feed");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DebuggerExtensionSetting.UserName))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify feed username");
            }

            else if (string.IsNullOrEmpty(baseConfiguration.DebuggerExtensionSetting.Token))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify feed token");
            }

            else if (!(new List<string> { "Debug", "Release" }.Contains(baseConfiguration.AppSetting.BuildConfig)))
            {
                throw new Exception(
                    $"{nameof(DebuggerExtensionTestConfigurationGenerator)}: Please specify valid build config");
            }

            return baseConfiguration;
        }

        public static DebuggerExtensionTestConfiguration GenerateConfiguration(string configFile)
        {
            DebuggerExtensionTestConfiguration configuration = ParseConfigFile(configFile);
            string testResultFolder = Path.Combine(configuration.Test.TestBed, "TestResult");

            foreach (string SDKVersion in configuration.SDKVersionList)
            {
                string testName = $"DebuggerExtension-SDK{SDKVersion}";
                string dumpFolder = Path.Combine(testResultFolder, $"dumps-sdk{SDKVersion}");
                string analysisOutputFolder = Path.Combine(testResultFolder, $"analysis-sdk{SDKVersion}");

                string dotNetRoot = Path.Combine(configuration.Test.TestBed, $"DotNetSDK-{SDKVersion}");

                string debuggerExtensionRoot = Path.Combine(configuration.Test.TestBed, $"dotnet-dump-{SDKVersion}");

                Dictionary<string, string> env = new();
                foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                {
                    env[de!.Key!.ToString()!] = de!.Value!.ToString()!;
                }
                env["DOTNET_ROOT"] = dotNetRoot;

                string targetAppsRoot = Path.Combine(testResultFolder, "TargetApps");
                string nativeaotAppRoot = Path.Combine(targetAppsRoot, $"nativeaot-sdk{SDKVersion}");

                DebuggerExtensionTestRunConfiguration runConfig = new()
                {
                    Test = new()
                    {
                        TestBed = configuration.Test.TestBed,
                        TestResultFolder = testResultFolder,
                        TestName = testName,
                        DumpFolder = dumpFolder,
                        AnalysisOutputFolder = analysisOutputFolder
                    },
                    SDKSetting = new()
                    {
                        Version = SDKVersion,
                        DotNetRoot = dotNetRoot,
                    },
                    DebuggerExtensionSetting = new()
                    {
                        Version = configuration.DebuggerExtensionSetting.Version,
                        Feed = configuration.DebuggerExtensionSetting.Feed,
                        ToolRoot = debuggerExtensionRoot,
                        UserName = configuration.DebuggerExtensionSetting.UserName,
                        Token = configuration.DebuggerExtensionSetting.Token
                    },
                    SysInfo = new()
                    {
                        EnvironmentVariables = new(env),
                        CLIDebugger = configuration.SysInfo.CLIDebugger
                    },
                    AppSetting = new()
                    {
                        BuildConfig = configuration.AppSetting.BuildConfig,
                        NativeAOTApp = new(env, "console", nativeaotAppRoot),
                    }
                };
            }
            return configuration;
        }
    }
}
