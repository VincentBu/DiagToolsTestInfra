using System.Collections;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class PerfCollect
    {
        private readonly string PerfCollectURL = 
            "https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect";
        private readonly string PerfCollectPath;

        public PerfCollect(string? perfcollectPath = null,
                           bool installPrerequisites = false)
        {
            if (!OperatingSystem.IsLinux())
            {
                throw new Exception($"{nameof(PerfCollect)}: perfcollect only works on Linux");
            }

            Dictionary<string, string> env = new();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                env[de!.Key!.ToString()!] = de!.Value!.ToString()!;
            }
            // Download perfcollect
            if (String.IsNullOrEmpty(perfcollectPath))
            {
                PerfCollectPath = Path.GetTempFileName();
            }
            else
            {
                PerfCollectPath = perfcollectPath;
            }

            Task.WaitAll(Utilities.Download(PerfCollectURL, PerfCollectPath));

            // Make the script executable
            CommandInvoker executableInvoker = new("chmod",
                                                   $"+x {PerfCollectPath}",
                                                   env,
                                                   "",
                                                   false);
            executableInvoker.WaitForResult();

            if (installPrerequisites)
            {
                CommandInvoker installInvoker = new("/bin/bash",
                                                    $"{PerfCollectPath} install",
                                                    env,
                                                    "",
                                                    false);
                installInvoker.WaitForResult();
            }
        }

        public CommandInvoker CollectTraceForSecs(Dictionary<string, string> env,
                                                  string tracePath,
                                                  int collectSecs,
                                                  string workingDirectory = "",
                                                  bool redirectStdOutErr = false,
                                                  bool silent = false)
        {
            return new("/bin/bash",
                       $"{PerfCollectPath} collect {tracePath} -collectsec {collectSecs}",
                       env,
                       workingDirectory,
                       redirectStdOutErr,
                       silent);
        }
    }
}
