namespace DiagToolsValidationRunner.Core.Functionality
{
    public static class DotNetTool
    {
        public static string GetToolIL(string toolRoot, string toolName, string toolVersion)
        {
            string frameworkRoot = Path.Combine(
                toolRoot, ".store", toolName, toolVersion, toolName, toolVersion, "tools");
            string frameworkDir =
                Directory.GetDirectories(frameworkRoot, "net*")
                    .First();
            string toolIL = Path.Combine(frameworkDir, "any", $"{toolName}.dll");

            if (!File.Exists(toolIL))
            {
                throw new Exception($"{nameof(DotNetTool)}: Tool IL {toolIL} doesn't exist");
            }

            return toolIL;
        }

        public static CommandInvokeResult InstallDotNetTool(Dictionary<string, string> dotNetEnv,
                                                       string toolRoot,
                                                       string toolFeed,
                                                       string toolVersion,
                                                       string toolName,
                                                       string? configFilePath=null,
                                                       bool redirectStdOutErr = true,
                                                       bool silent = false)
        {
            string dotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(dotNetEnv);
            string argument =
                configFilePath switch
                {
                    null => $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}",
                    _ => $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed} --configfile {configFilePath}"
                };

            CommandInvoker invoker = new(dotNetExecutable, argument, dotNetEnv, "", redirectStdOutErr, silent);
            return invoker.WaitForResult();
        }
    }
}
