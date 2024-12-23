open Spectre.Console
open Spectre.Console.Cli
open DiagToolsValidationToolSet.Command.DiagToolsTestRun

[<EntryPoint>]
let Main(args: string array) =
    try    
       let app = new CommandApp()

       app.Configure(fun configuration -> 
           configuration.AddCommand<DiagToolsTestRunCommand>("diagtoolstest-run") |> ignore
       )

       app.Run(args)
    with
        | ex -> raise ex