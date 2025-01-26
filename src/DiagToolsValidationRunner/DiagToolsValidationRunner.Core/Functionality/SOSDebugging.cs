using System.Diagnostics;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class SOSDebugger
    {
        private string cliDebugger;
        private string sosExtension;
        
        public SOSDebugger(string cliDebugger, string sosExtension)
        {
            this.cliDebugger = cliDebugger;
            this.sosExtension = sosExtension;
        }

        public string CLIDebugger
        {
            get { return cliDebugger; }
        }

        public string SOSExtension
        {
            get { return sosExtension; }
        }

        public void GenerateDebugScript(string targetRID, string scriptPath, List<string> basicSOSCommandList)
        {
            List<string> preRunCommandList = new();
            List<string> sosCommandList = new();
            List<string> exitCommandList = new();
            if (targetRID.Contains("win"))
            {
                preRunCommandList = [
                    ".unload sos",
                    $".load {sosExtension}"
                ];
                sosCommandList = basicSOSCommandList
                    .Select(command => $"!{command}")
                    .ToList();
                exitCommandList = [
                    ".detach",
                    "qq"
                ];
            }
            else
            {
                sosCommandList = basicSOSCommandList;
                exitCommandList = ["exit"];
            }

            List<string> debuggingCommandList = preRunCommandList
                .Concat(sosCommandList)
                .Concat(exitCommandList)
                .ToList();

            File.WriteAllLines(scriptPath, debuggingCommandList);
        }

        public CommandInvokeResult DebugDump(Dictionary<string, string> env,
                                             string targetRID,
                                             string workingDirectory,
                                             string dumpPath,
                                             string debuggerScriptPath,
                                             bool redirectStdOutErr = true,
                                             List<DataReceivedEventHandler>? outputHandlerList = null,
                                             List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            string arguments =
                targetRID.Contains("win") switch
                {
                    true => $"-cf {debuggerScriptPath} -z {dumpPath}",
                    false => $"-c {dumpPath} -s {debuggerScriptPath}"
                };

            using (CommandInvoker invoker = new(cliDebugger,
                                                arguments,
                                                env,
                                                workingDirectory))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }

        public CommandInvokeResult DebugAttachedProcess(Dictionary<string, string> env,
                                                        string targetRID,
                                                        string workingDirectory,
                                                        int pid,
                                                        string debuggerScriptPath,
                                                        bool redirectStdOutErr = true,
                                                        List<DataReceivedEventHandler>? outputHandlerList = null,
                                                        List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            string arguments =
                targetRID.Contains("win") switch
                {
                    true => $"-cf {debuggerScriptPath} -p {pid}",
                    false => $"-s {debuggerScriptPath} -p {pid}"
                };

            using (CommandInvoker invoker = new(cliDebugger,
                                                arguments,
                                                env,
                                                workingDirectory))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }

        public CommandInvokeResult DebugLaunchable(Dictionary<string, string> env,
                                                   string targetRID,
                                                   string workingDirectory,
                                                   string launchable,
                                                   string debuggerScriptPath,
                                                   bool redirectStdOutErr = true,
                                                   List<DataReceivedEventHandler>? outputHandlerList = null,
                                                   List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            string arguments =
                targetRID.Contains("win") switch
                {
                    true => $"-g -cf {debuggerScriptPath} {launchable}",
                    false => $"-s {debuggerScriptPath} -o \"run\"  {launchable}"
                };

            using (CommandInvoker invoker = new(cliDebugger,
                                                arguments,
                                                env,
                                                workingDirectory))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }
    }
}
