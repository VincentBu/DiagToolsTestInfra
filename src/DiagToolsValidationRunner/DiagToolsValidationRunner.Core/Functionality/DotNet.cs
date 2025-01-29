using System.Runtime.InteropServices;
using System.Text;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public static class DotNetInfrastructure
    {
        private static readonly List<string> ValidRIDList = new()
        {
            "win-x64", "win-x86", "win-arm64",
            "linux-x64", "linux-musl-x64", 
            "linux-arm64", "linux-musl-arm64",
            "linux-arm", "linux-musl-arm",
            "osx-x64", "osx-arm64",
        };

        public static readonly string CurrentRID = RuntimeInformation.RuntimeIdentifier;

        public static void CheckRID(string targetRID)
        {
            if (!ValidRIDList.Contains(targetRID))
            {
                throw new ArgumentException($"{nameof(DotNetInfrastructure)}: The given RID {targetRID} is invalid");
            }
        }

        public static string GetExcutableFileExtensionByRID(string targetRID)
        {
            CheckRID(targetRID);
            if (targetRID.StartsWith("win"))
            {
                return ".exe";
            }
            else
            {
                return "";
            }
        }

        public static string GetCompressionExtensionByRID(string targetRID)
        {
            CheckRID(targetRID);
            if (targetRID.StartsWith("win"))
            {
                return ".zip";
            }
            else
            {
                return ".tar.gz";
            }
        }

        internal static async Task<string> GenerateDotNetSDKDownloadLink(string sdkFullVersion, string targetRID)
        {
            List<string> AzureFeedList = [
                "https://dotnetcli.azureedge.net/dotnet",
                "https://dotnetbuilds.azureedge.net/public"
            ];
            string compressedPackageExtension = GetCompressionExtensionByRID(targetRID);

            foreach (var feed in AzureFeedList)
            {
                string productVersionQueryUrl = $"{feed}/Sdk/{sdkFullVersion}/sdk-productVersion.txt";
                using (HttpClient httpClient = new())
                {
                    try
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(productVersionQueryUrl);
                        response.EnsureSuccessStatusCode();
                        string content = await response.Content.ReadAsStringAsync();
                        string productVersion = content.Replace("\n", "").Replace("\r", "");
                        return $"{feed}/Sdk/{sdkFullVersion}/dotnet-sdk-{productVersion}-{targetRID}{compressedPackageExtension}";
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            // If fail to get ProductVersion with all feeds in AzureFeedList, throw exception
            throw new Exception($"{nameof(DotNetInfrastructure)}: Fail to find .Net SDK download link for {sdkFullVersion}");
        }

        public static void InstallDotNetSDKByVersion(string sdkFullVersion, string targetRID, string dotNetRoot)
        {
            string downloadLink = GenerateDotNetSDKDownloadLink(sdkFullVersion, targetRID).Result;
            string SDKExtension = GetCompressionExtensionByRID(targetRID);
            string downloadPath = Path.GetTempFileName() + SDKExtension;

            Utilities.Download(downloadLink, downloadPath).RunSynchronously();

            if (downloadPath.EndsWith(".tar.gz"))
            {
                Utilities.DecompressGzippedTar(downloadPath, dotNetRoot);
            }
            else if (downloadPath.EndsWith(".zip"))
            {
                Utilities.DecompressZip(downloadPath, dotNetRoot);
            }
            else
            {
                string extension = Path.GetExtension(downloadPath);
                throw new Exception($"{nameof(DotNetInfrastructure)}: Unknown extension {extension}");
            }
        }

        public static string GetDotNetExecutableFromEnv(Dictionary<string, string> env, string? targetRID=null)
        {
            if (!env.ContainsKey("DOTNET_ROOT"))
            {
                throw new Exception($"{nameof(DotNetInfrastructure)}: Please set DOTNET_ROOT");
            }

            string dotNetRoot = env["DOTNET_ROOT"];
            if (string.IsNullOrEmpty(targetRID))
            {
                targetRID = CurrentRID;
            }
            string exeExtension = GetExcutableFileExtensionByRID(targetRID);
            string dotNetExe = Path.Combine(dotNetRoot, $"dotnet{exeExtension}");

            if (!File.Exists(dotNetExe))
            {
                throw new Exception($"{nameof(DotNetInfrastructure)}: dotnet doesn't exist");
            }
            return dotNetExe;
        }

        public static void GenerateEnvironmentActivationScript(string targetRID, 
                                                               string scriptPath,
                                                               string dotNetRoot,
                                                               string? toolRoot=null)
        {
            StringBuilder content = new();
            if (targetRID.Contains("win"))
            {
                content.AppendLine($"$Env:DOTNET_ROOT=\"{dotNetRoot}\"");
                content.AppendLine($"$Env:Path+=\";{dotNetRoot}\"");
                if (!String.IsNullOrEmpty(toolRoot))
                {
                    content.AppendLine($"$Env:Path+=\";{toolRoot}\"");
                }
            }
            else
            {
                content.AppendLine($"export DOTNET_ROOT={dotNetRoot}");
                content.AppendLine($"export PATH=$PATH:{dotNetRoot}");
                if (!String.IsNullOrEmpty(toolRoot))
                {
                    content.AppendLine($"export PATH=$PATH:{toolRoot}");
                }
            }
            File.WriteAllText(scriptPath, content.ToString());
        }

        public static void ActiveDotNetDumpGeneratingEnvironment(Dictionary<string, string> env,
                                                                 string dumpPath)
        {
            env["DOTNET_DbgEnableMiniDump"] = "1";
            env["DOTNET_DbgMiniDumpType"] = "4";
            env["DOTNET_DbgMiniDumpName"] = dumpPath;
        }

        public static void ActiveWin32DumpGeneratingEnvironment(string dumpFolder)
        {
#if WINDOWS
            RegistryKey registrykeyHKLM = Registry.LocalMachine;
            string LocalDumpsKeyPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps";
            using (RegistryKey? LocalDumpsKey = registrykeyHKLM.OpenSubKey(LocalDumpsKeyPath, true))
            {
                LocalDumpsKey!.SetValue("DumpFolder", dumpFolder, RegistryValueKind.ExpandString);
                LocalDumpsKey!.SetValue("DumpType", 2, RegistryValueKind.DWord);
            }
#endif
        }

        public static void ActiveStressLogEnvironment(Dictionary<string, string> env,
                                                      string logFilePath)
        {
            env["DOTNET_StressLog"] = "1";
            env["DOTNET_StressLogLevel"] = "10";
            env["DOTNET_TotalStressLogSize"] = "8196";
            env["DOTNET_StressLogFilename"] = logFilePath;
        }
    }
}
