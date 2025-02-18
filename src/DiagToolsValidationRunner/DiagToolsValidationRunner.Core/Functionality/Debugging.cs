namespace DiagToolsValidationRunner.Core.Functionality
{
    public class CLIDebugger
    {
        public string CLIDebuggerPath { get; }
        
        public CLIDebugger(string cliDebuggerPath)
        {
            this.CLIDebuggerPath = cliDebuggerPath;
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
                    false => $"-c {dumpPath} -s {debuggerScriptPath} --batch"
                };

            CommandInvoker invoker = new(CLIDebuggerPath,
                                         arguments,
                                         env,
                                         workingDirectory);
            
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
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
                    false => $"-s {debuggerScriptPath} -p {pid}  --batch"
                };

            CommandInvoker invoker = new(CLIDebuggerPath,
                                         arguments,
                                         env,
                                         workingDirectory);
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
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
                    false => $"-s {debuggerScriptPath} --batch {launchable}"
                };

            CommandInvoker invoker = new(CLIDebuggerPath,
                                         arguments,
                                         env,
                                         workingDirectory);
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
        }
    }

    public class DotNetDumpAnalyzer
    {
        private string DotNetExecutablePath { get; }
        public string DotNetDumpILPath { get; }

        public DotNetDumpAnalyzer(string dotNetExecutablePath, string dotNetDumpILPath)
        {
            this.DotNetExecutablePath = dotNetExecutablePath;
            this.DotNetDumpILPath = dotNetDumpILPath;
        }

        public CommandInvokeResult DebugDump(Dictionary<string, string> env,
                                             string workingDirectory,
                                             string dumpPath,
                                             List<string> sosCommandList,
                                             bool redirectStdOutErr = true,
                                             bool silent = true)
        {
            CommandInvoker invoker = new(DotNetExecutablePath,
                                         $"{DotNetDumpILPath} analyze {dumpPath}",
                                         env,
                                         workingDirectory);
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            invoker.InvokeCommandWithOutWaitingForExit(redirectStdOutErr);
            int pid = invoker.InvokedProcess.Id;
            foreach (var command in sosCommandList)
            {
                invoker.InvokedProcess.StandardInput.WriteLine(command);
            }

            invoker.InvokedProcess.StandardInput.WriteLine("exit");
            invoker.InvokedProcess.WaitForExit();
            return new(invoker.Command, invoker.StandardOutput, invoker.StandardError, pid);
        }
    }
}
