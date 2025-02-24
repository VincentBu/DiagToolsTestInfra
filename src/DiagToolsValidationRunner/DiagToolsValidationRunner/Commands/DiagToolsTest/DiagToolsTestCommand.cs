using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiagToolsValidationRunner.Core.Configuration.DiagnosticsTest;
using DiagToolsValidationRunner.Core.TestRunner;
using Spectre.Console.Cli;

namespace DiagToolsValidationRunner.Commands.DiagToolsTest
{
    internal sealed class DiagToolsTestCommand:
        Command<DiagToolsTestCommand.DiagToolsTestSettings>
    {
        private readonly string _baseConsoleAppSrcPath =
            Path.Combine("Commands", "DiagToolsTest", "TargetAppsSrc", "consoleapp", "Program.cs.txt");

        private readonly string _baseGCDumpPlaygroundSrcPath =
            Path.Combine("Commands", "DiagToolsTest", "TargetAppsSrc", "GCDumpPlayground2", "Program.cs.txt");

        public sealed class DiagToolsTestSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute(CommandContext context, DiagToolsTestSettings settings)
        {
            DiagToolsTestRunConfiguration config =
                DiagToolsTestConfigurationGenerator.GenerateConfiguration(settings.ConfigurationPath);

            DiagToolsTestRunner runner = new(config, _baseConsoleAppSrcPath, _baseGCDumpPlaygroundSrcPath);
            return 0;
        }
    }
}
