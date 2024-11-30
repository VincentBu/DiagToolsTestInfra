namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System.IO
open System.Text
open System.Collections.Generic

open DiagToolsValidator.Core.CoreFunctionality


module TestInitializer =
    let CreateBuildTargetApps (env: Dictionary<string, string>) 
                              (dotNetRoot: string)
                              (loggerPath: string) 
                              (testBed: string)
                              (baseConsoleAppSrcPath)
                              (baseGCDumpPlaygroundSrcPath) =
        let trace = new Core.CommandInvokeTraceBuilder("Create and build target .NET apps", loggerPath)

        let commandInvoker = 
            trace {
                // Create console app
                let consoleAppRoot = Path.Combine(testBed, "console")
                trace.AppendLineToLogger loggerPath "Create Console App"
                let createConsoleAppCommandInvoker = DotNetApp.CreateDotNetApp env
                                                                               dotNetRoot
                                                                               "console"
                                                                               consoleAppRoot
                yield createConsoleAppCommandInvoker
            
                // Replace source file
                let targetConsoleAppSrcPath = Path.Combine(consoleAppRoot, "Program.cs")
                let consoleSrcContent = File.ReadAllText(baseConsoleAppSrcPath)
                File.WriteAllText(targetConsoleAppSrcPath, consoleSrcContent)
                
                // Build console app
                trace.AppendLineToLogger loggerPath "Build Console App"
                let buildConsoleAppCommandInvoker = DotNetApp.BuildDotNetApp env
                                                                    dotNetRoot
                                                                    "Debug"
                                                                    consoleAppRoot
                yield buildConsoleAppCommandInvoker

                // Create GCDumpPlayground
                let gcDumpPlaygroundRoot = Path.Combine(testBed, "GCDumpPlayground")
                trace.AppendLineToLogger loggerPath "Create GCDumpPlayground"
                let createGCDumpPlaygroundCommandInvoker = DotNetApp.CreateDotNetApp env
                                                                                     dotNetRoot
                                                                                     "console"
                                                                                     gcDumpPlaygroundRoot
                yield createGCDumpPlaygroundCommandInvoker
            
                // Replace source file
                let targetGCDumpPlaygroundSrcPath = Path.Combine(gcDumpPlaygroundRoot, "Program.cs")
                let gcDumpPlaygroundSrcContent = File.ReadAllText(baseGCDumpPlaygroundSrcPath)
                File.WriteAllText(targetGCDumpPlaygroundSrcPath, gcDumpPlaygroundSrcContent)
                
                // Build GCDumpPlayground
                trace.AppendLineToLogger loggerPath "Build GCDumpPlayground App"
                let buildGCDumpPlaygroundCommandInvoker = DotNetApp.BuildDotNetApp env
                                                                   dotNetRoot
                                                                   "Debug"
                                                                   gcDumpPlaygroundRoot
                yield buildGCDumpPlaygroundCommandInvoker
            
                // Create webapp
                let webappRoot = Path.Combine(testBed, "webapp")
                trace.AppendLineToLogger loggerPath "Create Webapp"
                let createWebappCommandInvoker = DotNetApp.CreateDotNetApp env
                                                                           dotNetRoot
                                                                           "webapp"
                                                                           webappRoot
                yield createWebappCommandInvoker
            
                // Build webapp
                trace.AppendLineToLogger loggerPath "Build Webapp"
                let buildWebappCommandInvoker = DotNetApp.BuildDotNetApp env
                                                                         dotNetRoot
                                                                         "Debug"
                                                                         webappRoot
                yield buildWebappCommandInvoker
            }
        if isNull(commandInvoker.Exception) && Core.IsNullOrEmptyString (commandInvoker.StandardError.ToString())
        then Choice1Of2 "Successfully create .NET apps for test"
        else Choice2Of2 (new exn($"CreateBuildTargetApps: Fail to run command: {commandInvoker.Command}"))


    let InstallDiagTools (env: Dictionary<string, string>)
                         (dotNetRoot: string)
                         (loggerPath: string) 
                         (toolRoot: string)
                         (toolFeed: string)
                         (toolVersion: string) 
                         (toolsToTest: string list) =
        let trace = new Core.CommandInvokeTraceBuilder("Create and build target .NET apps", loggerPath)

        let commandInvoker = 
            trace {
                for toolName in toolsToTest do
                    let invoker = DotNetTool.InstallDotNetTool env dotNetRoot toolRoot toolFeed toolVersion toolName
                    yield invoker
            }
            
        if isNull(commandInvoker.Exception) && Core.IsNullOrEmptyString (commandInvoker.StandardError.ToString())
        then Choice1Of2 "Successfully install diag tools for test"
        else Choice2Of2 (new exn($"InstallDiagTools: Fail to run command: {commandInvoker.Command}"))