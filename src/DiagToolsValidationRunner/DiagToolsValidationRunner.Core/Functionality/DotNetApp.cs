﻿using System.Diagnostics;
using System.Xml.Linq;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public class DotNetApp
    {
        private string appTemplate;
        private string appRoot;
        private string appName;
        private Dictionary<string, string> dotNetEnv;
        private string dotNetExecutable;

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
            if (!Directory.Exists(excutable))
            {
                throw new Exception($"{nameof(DotNetApp)}: Executable {excutable} doesn't exist in {symbolFolder}");
            }
            return excutable;
        }

        public static string GetAppNativeExecutable(string nativeSymbolFolder, string appName, string targetRID)
        {
            string excutableFileExtension = DotNetInfrastructure.GetExcutableFileExtensionByRID(targetRID);
            string excutable = Path.Combine(nativeSymbolFolder, $"{appName}{excutableFileExtension}");
            if (!Directory.Exists(excutable))
            {
                throw new Exception($"{nameof(DotNetApp)}: Native executable {excutable} doesn't exist in {nativeSymbolFolder}");
            }
            return excutable;
        }

        public DotNetApp(Dictionary<string, string> dotNetEnv,  string appTemplate, string appRoot, string? appName=null)
        {
            this.appTemplate = appTemplate;
            this.appRoot = appRoot;
            this.dotNetEnv = dotNetEnv;
            this.dotNetExecutable = dotNetEnv.GetValueOrDefault("DOTNET_ROOT", "dotnet");
            if (String.IsNullOrEmpty(appName))
            {
                this.appName = Path.GetFileName(appRoot);
            }
            else
            {
                this.appName = appName;
            }
        }

        public string GetProjectFilePath()
        {
            return DotNetApp.GetProjectFilePath(appRoot, appName);
        }

        public string GetTargetFramework()
        {
            string projectFilePath = this.GetProjectFilePath();
            return DotNetApp.GetTargetFramework(projectFilePath);
        }

        public string GetSymbolFolder(string buildConfig, string targetRID)
        {
            string targetFramework = this.GetTargetFramework();
            return DotNetApp.GetSymbolFolder(appRoot, targetFramework, buildConfig, targetRID);
        }

        public string GetNativeSymbolFolder(string buildConfig, string targetRID)
        {
            string targetFramework = this.GetTargetFramework();
            return DotNetApp.GetNativeSymbolFolder(appRoot, targetFramework, buildConfig, targetRID);
        }

        public string GetAppExecutable(string buildConfig, string targetRID)
        {
            string symbolFolder = this.GetSymbolFolder(buildConfig, targetRID);
            return DotNetApp.GetAppExecutable(symbolFolder, appName, targetRID);
        }

        public string GetNativeAppExecutable(string buildConfig, string targetRID)
        {
            string nativeSymbolFolder = this.GetSymbolFolder(buildConfig, targetRID);
            return DotNetApp.GetAppNativeExecutable(nativeSymbolFolder, appName, targetRID);
        }

        public CommandInvokeResult CreateApp(bool redirectStdOutErr=true,
                                             List<DataReceivedEventHandler>? outputHandlerList = null,
                                             List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            using (CommandInvoker invoker = new(dotNetExecutable,
                                                $"new {appTemplate} -o {appRoot} -n {appName}",
                                                dotNetEnv,
                                                ""))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }

        public CommandInvokeResult BuildApp(string buildConfig,
                                            string targetRID,
                                            bool redirectStdOutErr = true,
                                            List<DataReceivedEventHandler>? outputHandlerList = null,
                                            List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            using (CommandInvoker invoker = new(dotNetExecutable,
                                                $"build -r {targetRID} -c {buildConfig}",
                                                dotNetEnv,
                                                appRoot))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }

        public CommandInvokeResult PublishApp(string buildConfig,
                                              string targetRID,
                                              bool redirectStdOutErr = true,
                                              List<DataReceivedEventHandler>? outputHandlerList = null,
                                              List<DataReceivedEventHandler>? errorHandlerList = null)
        {
            using (CommandInvoker invoker = new(dotNetExecutable,
                                                $"publish -r {targetRID} -c {buildConfig}",
                                                dotNetEnv,
                                                appRoot))
            {
                return invoker.InvokeCommand(redirectStdOutErr, outputHandlerList, errorHandlerList);
            }
        }
    }
}
