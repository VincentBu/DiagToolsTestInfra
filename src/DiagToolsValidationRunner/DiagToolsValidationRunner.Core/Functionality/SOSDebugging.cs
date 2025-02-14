using System.Diagnostics;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class SOSDebugger
    {
        private string cliDebugger;
        
        public SOSDebugger(string cliDebugger)
        {
            this.cliDebugger = cliDebugger;
        }

        public string CLIDebugger
        {
            get { return cliDebugger; }
        }

        public void GenerateDebugScript(string targetRID, string scriptPath, List<string> basicSOSCommandList)
        {
            List<string> preRunCommandList = new();
            List<string> sosCommandList = new();
            List<string> exitCommandList = new();
            if (targetRID.Contains("win"))
            {
                string? userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
                string sosExtension = Path.Combine($"{userProfile}", ".dotnet", "sos", "sos.dll");
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
                                             bool silent = true)
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
                if (!silent)
                {
                    invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                    invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
                }
                return invoker.InvokeCommand(redirectStdOutErr);
            }
        }

        public CommandInvokeResult DebugAttachedProcess(Dictionary<string, string> env,
                                                        string targetRID,
                                                        string workingDirectory,
                                                        int pid,
                                                        string debuggerScriptPath,
                                                        bool redirectStdOutErr = true,
                                                        bool silent = true)
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
                if (!silent)
                {
                    invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                    invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
                }
                return invoker.InvokeCommand(redirectStdOutErr);
            }
        }

        public CommandInvokeResult DebugLaunchable(Dictionary<string, string> env,
                                                   string targetRID,
                                                   string workingDirectory,
                                                   string launchable,
                                                   string debuggerScriptPath,
                                                   bool redirectStdOutErr = true,
                                                   bool silent = true)
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
                if (!silent)
                {
                    invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                    invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
                }
                return invoker.InvokeCommand(redirectStdOutErr);
            }
        }
    }
}
