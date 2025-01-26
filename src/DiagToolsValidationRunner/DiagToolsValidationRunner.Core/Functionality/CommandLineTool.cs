using System.Diagnostics;
using System.Text;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class CommandInvokeResult
    {
        private readonly string command;
        private readonly string stdout;
        private readonly string stderr;
        private readonly int pid;

        public string Command
        {
            get { return command.ToString(); }
        }

        public string StandardOutput
        {
            get { return stdout.ToString(); }
        }

        public string StandardError
        {
            get { return stderr.ToString(); }
        }

        public int PID
        {
            get { return pid; }
        }

        public CommandInvokeResult(string command, string stdout, string stderr, int pid)
        {
            this.command = command;
            this.stdout = stdout;
            this.stderr = stderr;
            this.pid = pid;
        }
    }

    public class CommandInvoker: IDisposable
    {
        public static void PrintReceivedData(object sender, DataReceivedEventArgs args)
        {
            string? content = args.Data;
            if (!String.IsNullOrEmpty(content))
            {
                Console.WriteLine($"    {content}");
            }
        }
        
        private StringBuilder stdout;
        private StringBuilder stderr;
        private Process proc;

        public readonly string Command;

        public string CommandStandardOutput
        {
            get { return stdout.ToString(); }
        }

        public string CommandStandardError
        {
            get { return stderr.ToString(); }
        }

        public Process InvokedProcess
        {
            get { return proc; }
        }

        public CommandInvoker(string fileName,
                              string argument,
                              Dictionary<string, string> env,
                              string workDirectory = "")
        {
            // Initialize
            stdout = new();
            stderr = new();
            Command = $"{fileName} {argument}";
            proc = new();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.Arguments = argument;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.WorkingDirectory = workDirectory;

            proc.StartInfo.EnvironmentVariables.Clear();
            proc.StartInfo.EnvironmentVariables["Path"] = Environment.GetEnvironmentVariable("Path");
            foreach (var key in env.Keys)
            {
                proc.StartInfo.EnvironmentVariables[key] = env[key];
            }
        }

        public void InvokeCommandWithOutWaitingForExit(bool redirectStdOutErr,
                                                       List<DataReceivedEventHandler>? outputHandlerList = null,
                                                       List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            if (redirectStdOutErr)
            {
                proc.OutputDataReceived += (sender, args) =>
                {
                    string? line = args.Data;
                    if (!String.IsNullOrEmpty(line))
                    {
                        stdout.AppendLine(line);
                    }
                };
                proc.ErrorDataReceived += (sender, args) =>
                {
                    string? line = args.Data;
                    if (!String.IsNullOrEmpty(line))
                    {
                        stderr.AppendLine(line);
                    }
                };
            }

            proc.StartInfo.RedirectStandardOutput = redirectStdOutErr;
            proc.StartInfo.RedirectStandardError = redirectStdOutErr;

            if (outputHandlerList != null)
            {
                outputHandlerList.ForEach(outputHandler => proc.OutputDataReceived += outputHandler);
            }
            if (errorHandlerList != null)
            {
                errorHandlerList.ForEach(errorHandler => proc.ErrorDataReceived += errorHandler);
            }
            Console.WriteLine($"Run command: {Command}");
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }

        public CommandInvokeResult InvokeCommand(bool redirectStdOutErr,
                                                 List<DataReceivedEventHandler>? outputHandlerList = null, 
                                                 List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            InvokeCommandWithOutWaitingForExit(redirectStdOutErr, outputHandlerList, errorHandlerList);

            int pid = proc.Id;
            proc.WaitForExit();
            proc.Dispose();
            return new(Command, stdout.ToString(), stderr.ToString(), pid);
        }

        public void Dispose()
        {
            proc.Kill(true);
            proc.WaitForExit();
            proc.Dispose();
        }
    }
}
