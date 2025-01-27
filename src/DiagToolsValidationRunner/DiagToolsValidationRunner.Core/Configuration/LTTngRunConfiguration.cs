using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;

using DiagToolsValidationRunner.Core.Functionality;
using System.Collections;

namespace DiagToolsValidationRunner.Core.Configuration.LTTng
{

    public class TargetAppSetting : BaseTargetAppSetting
    {
        public DotNetApp? GCPerfsim;
    }

    public class LTTngRunConfiguration
    {
        public required BaseTestSetting Test;
        public required DotNetSDKSetting SDKSetting;
        public required TargetAppSetting AppSetting;
        public required BaseSystemInformation SysInfo;
    }

    public class LTTngConfiguration
    {
        public required BaseTestSetting Test;
        public required List<string> SDKVersionList;
        public required TargetAppSetting AppSetting;
        public List<LTTngRunConfiguration>? LTTngRunConfigurationList;
    }

    public static class LTTngTestConfigurationGenerator
    {
        private static IDeserializer _deserializer = (new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();
        
        private static LTTngConfiguration ParseConfigFile(string configFile)
        {
            try
            {
                string serializedConfiguration = File.ReadAllText(configFile);
                LTTngConfiguration baseConfiguration =
                    _deserializer.Deserialize<LTTngConfiguration>(configFile);

                if (string.IsNullOrEmpty(baseConfiguration.Test.TestBed))
                {
                    throw new Exception(
                        $"{nameof(LTTngTestConfigurationGenerator)}: Please specify testbed");
                }

                else if (!(new List<string> { "Debug", "Release" }.Contains(baseConfiguration.AppSetting.BuildConfig)))
                {
                    throw new Exception(
                        $"{nameof(LTTngTestConfigurationGenerator)}: Please specify valid build config");
                }

                else if (baseConfiguration.SDKVersionList.Count == 0)
                {
                    throw new Exception(
                        $"{nameof(LTTngTestConfigurationGenerator)}: Please specify at least one SDK Version");
                }
                
                return baseConfiguration;
            }
            catch
            {
                throw;
            }
        }

        public static LTTngConfiguration GenerateConfiguration(string configFile)
        {
            LTTngConfiguration configuration = ParseConfigFile(configFile);
            string testResultFolder = Path.Combine(configuration.Test.TestBed, "TestResult");

            configuration.LTTngRunConfigurationList = new();
            foreach (string SDKVersion in configuration.SDKVersionList)
            {
                string testName = $"LTTng-SDK{SDKVersion}";
                string dotNetRoot = Path.Combine(configuration.Test.TestBed, $"DotNetSDK-{SDKVersion}");

                Dictionary<string, string> env = new();
                foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                {
                    env[de!.Key!.ToString()!] = de!.Value!.ToString()!;
                }
                env["DOTNET_ROOT"] = dotNetRoot;

                string targetAppsRoot = Path.Combine(configuration.Test.TestBed, "TargetApps");
                string appRoot = Path.Combine(targetAppsRoot, $"gcperfsim-sdk{SDKVersion}-{DotNetInfrastructure.CurrentRID}");

                LTTngRunConfiguration runConfig = new()
                {
                    Test = new()
                    {
                        TestBed = configuration.Test.TestBed,
                        TestResultFolder = testResultFolder,
                        TestName = testName
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
                        GCPerfsim = new(env, "console", appRoot)
                    }
                };

                configuration.LTTngRunConfigurationList.Add(runConfig);
            }

            return configuration;
        }
    }
}
