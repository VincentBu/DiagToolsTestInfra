using System.Diagnostics;

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
                                                       List<DataReceivedEventHandler>? outputHandlerList = null,
                                                       List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            string dotNetExecutable = dotNetEnv.GetValueOrDefault("DOTNET_ROOT", "dotnet");
            string argument =
                configFilePath switch
                {
                    null => $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}",
                    _ => $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed} --configfile {configFilePath}"
                };

            using (CommandInvoker invoker = new(
                dotNetExecutable, argument, dotNetEnv, ""))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }

        public static async Task DownloadPerfcollect(string perfcollectPath)
        {
            string perfcollectUrl = "https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect";
            await Utilities.Download(perfcollectUrl, perfcollectPath);
        }
    }
}
