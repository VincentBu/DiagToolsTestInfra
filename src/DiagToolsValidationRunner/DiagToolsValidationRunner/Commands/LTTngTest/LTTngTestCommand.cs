using System.ComponentModel;

using Spectre.Console.Cli;

using DiagToolsValidationRunner.Core.Configuration.LTTngTest;
using DiagToolsValidationRunner.Core.TestRunner.LTTngTest;
using DiagToolsValidationRunner.Core.Functionality;

namespace DiagToolsValidationRunner.Commands.LTTngTest
{
    internal sealed class LTTngTestCommand:
        Command<LTTngTestCommand.LTTngTestSettings>
    {
        private readonly string _baseGCPerfsimAppSrcPath =
            Path.Combine("Commands", "LTTngTest", "TargetAppsSrc", "gcperfsim", "Program.cs.txt");

        public sealed class LTTngTestSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute(CommandContext context, LTTngTestSettings setting)
        {
            LTTngTestConfiguration config =
                LTTngTestConfigurationGenerator.GenerateConfiguration(setting.ConfigurationPath);

            Directory.CreateDirectory(config.Test.TestBed);
            string perfcollectPath = Path.Combine(config.Test.TestBed, "perfcollect"); 
            PerfCollect perfcollect = new(perfcollectPath, true);
            foreach (var runConfig in config.LTTngRunConfigurationList)
            {
                LTTngTestRunner testRunner = new(runConfig, _baseGCPerfsimAppSrcPath);
                testRunner.TestLTTng(perfcollect);
            }
            return 0;
        }
    }
}
