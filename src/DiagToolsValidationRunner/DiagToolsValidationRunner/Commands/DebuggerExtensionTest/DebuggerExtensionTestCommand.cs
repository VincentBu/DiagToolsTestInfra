using System.ComponentModel;

using Spectre.Console.Cli;

using DiagToolsValidationRunner.Core.Configuration.DebuggerExtensionTest;
using DiagToolsValidationRunner.Core.TestRunner.DebuggerExtensionTest;

namespace DiagToolsValidationRunner.Commands.DebuggerExtensionTest
{
    internal sealed class DebuggerExtensionTestCommand:
        Command<DebuggerExtensionTestCommand.DebuggerExtensionTestSettings>
    {
        private readonly string _baseNativeAOTAppSrcPath = 
            Path.Combine("Commands", "DebuggerExtensionTest", "TargesAppsSrc", "nativeaot", "Program.cs.txt");

        public sealed class DebuggerExtensionTestSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute(CommandContext context, DebuggerExtensionTestSettings settings)
        {
            DebuggerExtensionTestConfiguration config =
                DebuggerExtensionTestConfigurationGenerator.GenerateConfiguration(settings.ConfigurationPath);
            foreach (var runConfig in config.DebuggerExtensionTestRunConfigurationList)
            {
                DebuggerExtensionTestRunner testRunner = new(runConfig, _baseNativeAOTAppSrcPath);
                testRunner.TestDebuggerExtension();
            }

            return 0;
        }

        
    }
}