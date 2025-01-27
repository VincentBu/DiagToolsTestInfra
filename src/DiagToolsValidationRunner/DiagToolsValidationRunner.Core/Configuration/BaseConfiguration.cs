namespace DiagToolsValidationRunner.Core.Configuration
{
    public class BaseSystemInformation
    {
        public Dictionary<string, string>? EnvironmentVariables;
    }

    public class BaseTestSetting
    {
        public string? TestName;
        public required string TestBed;
        public string? TestResultFolder;
    }

    public class DotNetSDKSetting
    {
        public required string Version;
        public string? DotNetRoot;
    }

    public class DotNetToolSetting
    {
        public required string Version;
        public required string Feed;
        public string? ToolRoot;
    }

    public class BaseTargetAppSetting
    {
        public required string BuildConfig;
    }
}
