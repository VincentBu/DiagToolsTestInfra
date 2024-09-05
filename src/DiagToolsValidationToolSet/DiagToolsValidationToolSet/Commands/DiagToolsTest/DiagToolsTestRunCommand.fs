namespace DiagToolsValidationToolSet.Command

open Spectre.Console
open Spectre.Console.Cli
open DiagToolsValidationToolSet.Core.Configuration.DiagToolsTestRun
open DiagToolsValidationToolSet.Core.Function.DotNetSDKAndRuntime

module DiagToolsTestRun =
    type DiagToolsTestRunSettings() =
        inherit CommandSettings()

        [<CommandOption("-c|--configuration")>]
        member val ConfigurationPath: string = "" with get, set


    type DiagToolsTestRunCommand() =
        inherit Command<DiagToolsTestRunSettings>()

        override this.Execute(context: CommandContext, setting: DiagToolsTestRunSettings) =
            AnsiConsole.Write(new Rule("Start Diagnostic Tools Test"));
            AnsiConsole.WriteLine();

            let baseConfiguration = DiagToolsTestRunConfigurationGenerator.ParseConfigFile setting.ConfigurationPath
            0