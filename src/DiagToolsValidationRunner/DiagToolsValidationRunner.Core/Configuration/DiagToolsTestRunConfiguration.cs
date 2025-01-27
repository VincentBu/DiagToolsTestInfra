using System.Text;
using System.Collections;

using YamlDotNet.Serialization;

using DiagToolsValidationRunner.Core.Functionality;


namespace DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest
{
    public class TestSetting: BaseTestSetting
    {
        public required bool OptionalFeatureContainer;
    }

    public class SystemInformation: BaseSystemInformation
    {
        public required string OSName;
        public required string CPUArchitecture;
        public required string CLIDebugger;
    }

    public class DiagToolSetting : DotNetToolSetting
    {
        public required List<string> ToolsToTest;
    }

    public class TargetAppSetting: BaseTargetAppSetting
    {
        public DotNetApp? ConsoleApp;
        public DotNetApp? WebApp;
        public DotNetApp? GCDumpPlayground;
    }

    public class DiagToolsTestRunConfiguration
    {
        public required TestSetting Test;
        public required DotNetSDKSetting SDKSetting;
        public required DiagToolSetting ToolSetting;
        public required TargetAppSetting AppSetting;
        public required SystemInformation SysInfo;
    }

    public static class DiagToolsTestConfigurationGenerator
    {
        private static IDeserializer _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();

        private static DiagToolsTestRunConfiguration ParseConfigFile(string configFile)
        {
            try
            {
                string serializedConfiguration = File.ReadAllText(configFile);
                DiagToolsTestRunConfiguration configuration =
                    _deserializer.Deserialize<DiagToolsTestRunConfiguration>(configFile);

                if (string.IsNullOrEmpty(configuration.SDKSetting.Version))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify .NET SDK version");
                }
                else if (string.IsNullOrEmpty(configuration.Test.TestBed))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify testbed");
                }

                else if (string.IsNullOrEmpty(configuration.ToolSetting.Version))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify diag tool version");
                }

                else if (string.IsNullOrEmpty(configuration.ToolSetting.Feed))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify diag tool feed");
                }

                else if (!(new List<string>{"Debug", "Release"}.Contains(configuration.AppSetting.BuildConfig)))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify valid build config");
                }

                else if (string.IsNullOrEmpty(configuration.SysInfo.OSName))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify OS name");
                }

                else if (string.IsNullOrEmpty(configuration.SysInfo.CPUArchitecture))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify processor architecture");
                }

                else if (string.IsNullOrEmpty(configuration.SysInfo.CLIDebugger))
                {
                    throw new Exception(
                        $"{nameof(DiagToolsTestConfigurationGenerator)}: Please specify debugger");
                }

                return configuration;
            }
            catch
            {
                throw;
            }
        }

        public static DiagToolsTestRunConfiguration GenerateConfiguration(string configFile)
        {
            DiagToolsTestRunConfiguration configuration = ParseConfigFile(configFile);

            // Initialize Test
            StringBuilder testName = new();
            testName.Append($"-{configuration.SysInfo.OSName}");
            testName.Append($"-{configuration.SysInfo.CPUArchitecture}");
            testName.Append($"-SDK{configuration.SDKSetting.Version}");
            testName.Append($"-Tool{configuration.ToolSetting.Version}");
            if (configuration.Test.OptionalFeatureContainer)
            {
                testName.Append("");
            }
            else
            {
                testName.Append("-NO");
            }
            configuration.Test.TestName = testName.ToString();
            configuration.Test.TestResultFolder = Path.Combine(
                configuration.Test.TestBed, $"TestResult-{configuration.Test.TestName}");

            // Initialize SDKSetting
            configuration.SDKSetting.DotNetRoot = Path.Combine(
                configuration.Test.TestBed, "DotNetSDK");

            // Initialize ToolSetting
            configuration.ToolSetting.ToolRoot = Path.Combine(
                configuration.Test.TestBed, "DiagTools");

            // Initialize Environment
            configuration.SysInfo.EnvironmentVariables = new();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                configuration.SysInfo.EnvironmentVariables[de!.Key!.ToString()!] = de!.Value!.ToString()!;
            }
            configuration.SysInfo.EnvironmentVariables["DOTNET_ROOT"] = configuration.SDKSetting.DotNetRoot;

            // Initialize Target Apps
            string targetAppsRoot = Path.Combine(configuration.Test.TestBed, "TargetApps");
            configuration.AppSetting.ConsoleApp = new(
                configuration.SysInfo.EnvironmentVariables, "console", Path.Combine(targetAppsRoot, "console"));
            configuration.AppSetting.WebApp = new(
                configuration.SysInfo.EnvironmentVariables, "webapp", Path.Combine(targetAppsRoot, "webapp"));
            configuration.AppSetting.GCDumpPlayground = new(
                configuration.SysInfo.EnvironmentVariables, "console", Path.Combine(targetAppsRoot, "GCDumpPlayground"));

            return configuration;
        }
    }
}
