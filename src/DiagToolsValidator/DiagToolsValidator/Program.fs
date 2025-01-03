﻿namespace DiagToolsValidationToolSet

open Spectre.Console.Cli

open DiagToolsValidator.Command

module Main =

    [<EntryPoint>]
    let Main(args: string array) =
        try    
           let app = new CommandApp()

           app.Configure(fun configuration -> 
               configuration.AddCommand<DiagToolsTestRun.DiagToolsTestRunCommand>("diagtoolstest-run") |> ignore
               configuration.AddCommand<DebuggerExtensionTestRun.DebuggerExtensionTestRunCommand>("debuggerexttest-run") |> ignore
           )

           app.Run(args)
        with
            | ex -> raise ex