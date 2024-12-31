namespace DiagToolsValidator.Core.Task.DebuggerExtension

open System
open System.IO
open Microsoft.Win32
open System.Threading
open System.Xml.Linq
open System.Collections.Generic

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration


module TestInfrastructure =
    let GenerateEnvironmentActivationScript (configuration: DebuggerExtensionTestConfiguration.DebuggerExtensionTestRunConfiguration) =
        let scriptPath, lines =
            if DotNet.CurrentRID.Contains "win"
            then Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.ps1"), [
                $"$Env:DOTNET_ROOT=\"{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DebuggerExtension.ToolRoot}\""
            ]
            else Path.Combine(configuration.TestBed, $"env_activation_SDK{configuration.DotNet.SDKVersion}.sh"),[
                $"export DOTNET_ROOT={configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DebuggerExtension.ToolRoot}"
            ]

        try
            File.WriteAllLines(scriptPath, lines)
            Choice1Of2 scriptPath
        with ex -> 
            ex.Data.Add("", $"Fail to generate environment activation script for SDK-{configuration.DotNet.SDKVersion}.")
            Choice2Of2 ex


    let GenerateNugetConfig (configuration: DebuggerExtensionTestConfiguration.DebuggerExtensionTestRunConfiguration) =
        let nugetPath = Path.Combine(configuration.TargetApp.NativeAOTApp.AppRoot, "NuGet.config")
        let configContent = $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="dotnet10" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json" />
    <add key="dotnet9" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json" />
    <add key="dotnet8" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json" />
    <add key="internalPackageSource" value="{configuration.DebuggerExtension.Feed}" />
  </packageSources>
  <packageSourceCredentials>
    <internalPackageSource>
        <add key="Username" value="{configuration.DebuggerExtension.UserName}" /> 
        <add key="CleartextPassword" value="{configuration.DebuggerExtension.Token}" />
    </internalPackageSource>
  </packageSourceCredentials>
</configuration>"""
        try
            let nugetConfig = XElement.Parse(configContent)
            nugetConfig.Save(nugetPath)
            Choice1Of2 nugetPath
        with ex ->
            ex.Data.Add("", $"Fail to generate nuget config for SDK-{configuration.DotNet.SDKVersion}.")
            Choice2Of2 ex
     

    let ActiveDumpGeneratingEnvironment (env: Dictionary<string, string>) (dumpFolder: string) =
        let _env = new Dictionary<string, string>(env)
        
        if DotNet.CurrentRID.Contains("win")
        then 
            try
                // windows case: set reg keys
                let registrykeyHKLM = Registry.LocalMachine
                let LocalDumpsKeyPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps"
                let LocalDumpsKey = registrykeyHKLM.OpenSubKey(LocalDumpsKeyPath, true)
                LocalDumpsKey.SetValue("DumpFolder", dumpFolder, RegistryValueKind.ExpandString)
                LocalDumpsKey.SetValue("DumpType", 2, RegistryValueKind.DWord)
                LocalDumpsKey.Close()
                Choice1Of2 _env
            with ex ->
                ex.Data.Add("", $"Fail to set registry key.")
                Choice2Of2 ex
        else
            _env["DOTNET_DbgEnableMiniDump"] <- "1"
            _env["DOTNET_DbgMiniDumpType"] <- "4"
            _env["DOTNET_DbgMiniDumpName"] <- 
                Path.Combine(dumpFolder, $"nativedump.dmp")
            Choice1Of2 _env


    let ActiveStressLogEnvironment (env: Dictionary<string, string>) =
        let _env = new Dictionary<string, string>(env)
        _env["DOTNET_StressLogLevel"] <- "10"
        _env["DOTNET_TotalStressLogSize"] <- "8196"
        _env
        

    let RunNativeAOTApp (configuration: DebuggerExtensionTestConfiguration.DebuggerExtensionTestRunConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)

        trace {
            let! env = ActiveDumpGeneratingEnvironment configuration.SystemInfo.EnvironmentVariables configuration.TestResultFolder
            let env = ActiveStressLogEnvironment env
            let! executablePath = configuration.TargetApp.NativeAOTApp.GetAppNativeExecutable configuration.TargetApp.BuildConfig
            
            let! srcCreateDumpPath = configuration.TargetApp.NativeAOTApp.GetCreateDump configuration.TargetApp.BuildConfig
            let! nativeSymbolFolder = configuration.TargetApp.NativeAOTApp.GetNativeSymbolFolder configuration.TargetApp.BuildConfig
            Core.CopyFile srcCreateDumpPath nativeSymbolFolder |> ignore

            let invoker = CommandLineTool.RunCommand executablePath
                                                     ""
                                                     ""
                                                     env
                                                     true
                                                     CommandLineTool.IgnoreOutputData
                                                     CommandLineTool.IgnoreErrorData
                                                     true
             
            if DotNet.CurrentRID.Contains("win")
            then // windows case: delete reg keys
                let registrykeyHKLM = Registry.CurrentUser
                let keyPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps"

                registrykeyHKLM.DeleteSubKey($@"{keyPath}\DumpFolder", false)
                registrykeyHKLM.DeleteSubKey($@"{keyPath}\DumpType", false)
            |> ignore
            
            let dumpFileList = Directory.GetFiles(configuration.TestResultFolder, "*.dmp")

            let! dumpPath = 
                if Array.isEmpty dumpFileList
                then Choice2Of2 (new exn("No dump is generated"))
                else Choice1Of2 (dumpFileList[0])

            return dumpPath
        }
