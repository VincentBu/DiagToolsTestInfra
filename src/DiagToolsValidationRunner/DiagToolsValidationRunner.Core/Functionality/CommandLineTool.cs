using System.Diagnostics;
using System.Text;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class CommandInvokeResult
    {
        public string Command { get; init; }
        public string StandardOutput { get; init; }
        public string StandardError { get; init; }
        public int ProcessID { get; init; }
        public Exception? Exn { get; init; }

        public CommandInvokeResult(string command, string stdout, string stderr, int pid, Exception? ex = null)
        {
            this.Command = command;
            this.StandardOutput = stdout;
            this.StandardError = stderr;
            this.ProcessID = pid;
            this.Exn = ex;
        }
    }

    public static class CommandInvokeTaskRunner
    {
        public static void Run(string loggerPath,
                               IEnumerable<CommandInvokeResult> commandInvokeTask,
                               bool ignoreError = false)
        {
            IEnumerator<CommandInvokeResult> enumrator = commandInvokeTask.GetEnumerator();
            while (true)
            {
                try
                {
                    if (!enumrator.MoveNext())
                    {
                        // Break when move to end
                        break;
                    }
                    CommandInvokeResult result = enumrator.Current;
                    StringBuilder logContent = new();
                    logContent.AppendLine($"Run Command: {result.Command}");
                    logContent.AppendLine(result.StandardOutput);
                    logContent.AppendLine(result.StandardError);
                    if (result.Exn != null)
                    {
                        logContent.AppendLine($"Error Message:{result.Exn.Message}");
                        logContent.AppendLine($"Stack Trace:\n{result.Exn.StackTrace}");
                        logContent.AppendLine($"Inner Exception:\n:{result.Exn.InnerException}");
                    }
                    logContent.AppendLine("\n");
                    File.AppendAllText(loggerPath, logContent.ToString());

                    if (!String.IsNullOrEmpty(result.StandardError) || result.Exn != null)
                    {
                        if (!ignoreError)
                        {
                            Console.WriteLine($"Run Command {result.Command} but get error! See {loggerPath} for details.");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    StringBuilder logContent = new();
                    logContent.AppendLine($"Run into error: {ex.Message}");
                    logContent.AppendLine($"Stack Trace:\n{ex.StackTrace}");
                    logContent.AppendLine($"Inner Exception:\n{ex.InnerException}");
                    File.AppendAllText(loggerPath, logContent.ToString());
                    break;
                }
            }
        }

        public static void RecordSingle(string loggerPath, CommandInvokeResult result)
        {
            StringBuilder logContent = new();
            logContent.AppendLine($"Run Command: {result.Command}");
            logContent.AppendLine(result.StandardOutput);
            logContent.AppendLine(result.StandardError);
            if (result.Exn != null)
            {
                logContent.AppendLine($"Error Message:{result.Exn.Message}");
                logContent.AppendLine($"Stack Trace:\n{result.Exn.StackTrace}");
                logContent.AppendLine($"Inner Exception:\n:{result.Exn.InnerException}");
            }
            logContent.AppendLine("\n");
            File.AppendAllText(loggerPath, logContent.ToString());
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

        public string StandardOutput
        {
            get { return stdout.ToString(); }
        }

        public string StandardError
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

        public void InvokeCommandWithOutWaitingForExit(bool redirectStdOutErr = true)
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

            Console.WriteLine($"\nRun command: {Command}");
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }

        public CommandInvokeResult InvokeCommand(bool redirectStdOutErr)
        {
            try
            {
                InvokeCommandWithOutWaitingForExit(redirectStdOutErr);

                int pid = proc.Id;
                proc.WaitForExit();
                proc.Dispose();
                return new(Command, stdout.ToString(), stderr.ToString(), pid);
            }
            catch (Exception ex)
            {
                return new(Command, stdout.ToString(), stderr.ToString(), -1, ex);
            }
        }

        public void Dispose()
        {
            proc.Kill(true);
            proc.WaitForExit();
            proc.Dispose();
        }
    }
}
