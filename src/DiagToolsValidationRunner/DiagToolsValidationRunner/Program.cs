using Spectre.Console;
using Spectre.Console.Cli;

using DiagToolsValidationRunner.Commands.DebuggerExtensionTest;

namespace DiagToolsValidationRunner
{
    public sealed class Program()
    {
        public static void Main(string[] args)
        {
            try
            {
                var app = new CommandApp();
                app.Configure((configuration) =>
                {
                    // Run 
                    configuration.AddCommand<DebuggerExtensionTestCommand>("debuggerext-test");
                });

                app.Run(args);
            }

            // TODO: Handle each exception.
            catch (Exception ex)
            {
                AnsiConsole.Markup($"[bold red] Error Message:[/] {ex.Message}\n");
                AnsiConsole.Markup($"[bold red] Inner Exception:\n[/] {ex.InnerException}\n");
            }
        }
    }
}
