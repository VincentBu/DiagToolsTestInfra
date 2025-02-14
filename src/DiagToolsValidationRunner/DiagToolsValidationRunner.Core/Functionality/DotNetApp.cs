using System.Diagnostics;
using System.Xml.Linq;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class DotNetApp
    {
        public string AppTemplate { get; } = String.Empty;
        public string AppRoot { get; } = String.Empty;
        public string AppName { get; } = String.Empty;
        public Dictionary<string, string> DotNetEnv { get; } = new();
        private string DotNetExecutable = String.Empty;

        public static string GetProjectFilePath(string appRoot, string appName)
        {
            string projectFile = Path.Combine(appRoot, $"{appName}.csproj");
            if (!File.Exists(projectFile))
            {
                throw new Exception($"{nameof(DotNetApp)}: Project file {projectFile} doesn't exist in {appRoot}");
            }
            return projectFile;
        }

        public static string GetTargetFramework(string projectFilePath)
        {
            string xmlData = File.ReadAllText(projectFilePath);
            XDocument doc = XDocument.Parse(xmlData);
            return doc.Root!.Element("PropertyGroup")!.Element("TargetFramework")!.Value;
        }

        public static string GetSymbolFolder(string appRoot, string targetFramework, string buildConfig, string targetRID)
        {
            string appSymbolFolder = Path.Combine(appRoot, "bin", buildConfig, targetFramework, targetRID);
            if (!Directory.Exists(appSymbolFolder))
            {
                throw new Exception($"{nameof(DotNetApp)}: Symbol folder {appSymbolFolder} doesn't exist in {appRoot}");
            }
            return appSymbolFolder;
        }

        public static string GetNativeSymbolFolder(string appRoot, string targetFramework, string buildConfig, string targetRID)
        {
            string appNativeSymbolFolder = Path.Combine(appRoot, "bin", buildConfig, targetFramework, targetRID, "publish");
            if (!Directory.Exists(appNativeSymbolFolder))
            {
                throw new Exception($"{nameof(DotNetApp)}: Symbol folder {appNativeSymbolFolder} doesn't exist in {appRoot}");
            }
            return appNativeSymbolFolder;
        }

        public static string GetAppExecutable(string symbolFolder, string appName, string targetRID)
        {
            string excutableFileExtension = DotNetInfrastructure.GetExcutableFileExtensionByRID(targetRID);
            string excutable = Path.Combine(symbolFolder, $"{appName}{excutableFileExtension}");
            if (!File.Exists(excutable))
            {
                throw new Exception($"{nameof(DotNetApp)}: Executable {excutable} doesn't exist in {symbolFolder}");
            }
            return excutable;
        }

        public static string GetAppNativeExecutable(string nativeSymbolFolder, string appName, string targetRID)
        {
            string excutableFileExtension = DotNetInfrastructure.GetExcutableFileExtensionByRID(targetRID);
            string excutable = Path.Combine(nativeSymbolFolder, $"{appName}{excutableFileExtension}");
            if (!File.Exists(excutable))
            {
                throw new Exception($"{nameof(DotNetApp)}: Native executable {excutable} doesn't exist in {nativeSymbolFolder}");
            }
            return excutable;
        }

        public DotNetApp()
        {
            // Empty app as placeholder
        }

        public DotNetApp(Dictionary<string, string> dotNetEnv,  string appTemplate, string appRoot, string? appName=null)
        {
            this.AppTemplate = appTemplate;
            this.AppRoot = appRoot;
            this.DotNetEnv = dotNetEnv;

            this.DotNetExecutable = DotNetInfrastructure.GetDotNetExecutableFromEnv(dotNetEnv, null);
            if (String.IsNullOrEmpty(appName))
            {
                this.AppName = Path.GetFileName(AppRoot);
            }
            else
            {
                this.AppName = appName;
            }
        }

        public string GetProjectFilePath()
        {
            return DotNetApp.GetProjectFilePath(AppRoot, AppName);
        }

        public string GetTargetFramework()
        {
            string projectFilePath = this.GetProjectFilePath();
            return DotNetApp.GetTargetFramework(projectFilePath);
        }

        public string GetSymbolFolder(string buildConfig, string targetRID)
        {
            string targetFramework = this.GetTargetFramework();
            return DotNetApp.GetSymbolFolder(AppRoot, targetFramework, buildConfig, targetRID);
        }

        public string GetNativeSymbolFolder(string buildConfig, string targetRID)
        {
            string targetFramework = this.GetTargetFramework();
            return DotNetApp.GetNativeSymbolFolder(AppRoot, targetFramework, buildConfig, targetRID);
        }

        public string GetAppExecutable(string buildConfig, string targetRID)
        {
            string symbolFolder = this.GetSymbolFolder(buildConfig, targetRID);
            return DotNetApp.GetAppExecutable(symbolFolder, AppName, targetRID);
        }

        public string GetNativeAppExecutable(string buildConfig, string targetRID)
        {
            string nativeSymbolFolder = this.GetSymbolFolder(buildConfig, targetRID);
            return DotNetApp.GetAppNativeExecutable(nativeSymbolFolder, AppName, targetRID);
        }

        public CommandInvokeResult CreateApp(bool redirectStdOutErr = true, bool silent = false)
        {
            CommandInvoker invoker = new(DotNetExecutable,
                                         $"new {AppTemplate} -o {AppRoot} -n {AppName} --force",
                                         DotNetEnv,
                                         "");
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
        }

        public CommandInvokeResult BuildApp(string buildConfig,
                                            string targetRID,
                                            bool redirectStdOutErr = true,
                                            bool silent = false)
        {
            CommandInvoker invoker = new(DotNetExecutable,
                                         $"build -r {targetRID} -c {buildConfig}",
                                         DotNetEnv,
                                         AppRoot);
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
            
        }

        public CommandInvokeResult PublishApp(string buildConfig,
                                              string targetRID,
                                              bool redirectStdOutErr = true,
                                              bool silent = false)
        {
            CommandInvoker invoker = new(DotNetExecutable,
                                         $"publish -r {targetRID} -c {buildConfig}",
                                         DotNetEnv,
                                         AppRoot);
            if (!silent)
            {
                invoker.InvokedProcess.OutputDataReceived += CommandInvoker.PrintReceivedData;
                invoker.InvokedProcess.ErrorDataReceived += CommandInvoker.PrintReceivedData;
            }
            return invoker.InvokeCommand(redirectStdOutErr);
        }
    }
}
