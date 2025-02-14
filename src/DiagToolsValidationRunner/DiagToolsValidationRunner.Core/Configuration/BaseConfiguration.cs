namespace DiagToolsValidationRunner.Core.Configuration
{
    public class BaseSystemInformation
    {
        public Dictionary<string, string> EnvironmentVariables = new();
    }

    public class BaseTestSetting
    {
        public string TestName = String.Empty;
        public required string TestBed;
        public string TestResultFolder = String.Empty;
    }

    public class DotNetSDKSetting
    {
        public required string Version;
        public string DotNetRoot = String.Empty;
    }

    public class DotNetToolSetting
    {
        public required string Version;
        public required string Feed;
        public string ToolRoot = String.Empty;
    }

    public class BaseTargetAppSetting
    {
        public required string BuildConfig;
    }
}
