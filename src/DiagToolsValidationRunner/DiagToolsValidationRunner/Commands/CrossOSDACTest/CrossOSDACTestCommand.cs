using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiagToolsValidationRunner.Core.Configuration.CrossOSDACTest;
using DiagToolsValidationRunner.Core.TestRunner.CrossOSDACTest;

namespace DiagToolsValidationRunner.Commands.CrossOSDACTest
{
    internal sealed class CrossOSDACTestCommand:
        Command<CrossOSDACTestCommand.CrossOSDACTestSettings>
    {
        private readonly string _baseOOMAppSrcPath =
            Path.Combine("Commands", "CrossOSDACTest", "TargesAppsSrc", "oom", "Program.cs.txt");

        private readonly string _baseUHEAppSrcPath =
            Path.Combine("Commands", "CrossOSDACTest", "TargetAppsSrc", "uhe", "Program.cs.txt");

        public sealed class CrossOSDACTestSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public required string ConfigurationPath { get; init; }
        }

        public override int Execute(CommandContext context, CrossOSDACTestSettings setting)
        {
            CrossOSDACTestConfiguration config =
                CrossOSDACTestConfigurationGenerator.GenerateConfiguration(setting.ConfigurationPath);

            foreach (var runConfig in config.CrossOSDACTestRunConfigurationList)
            {
                CrossOSDACTestRunner runner = new(runConfig, _baseOOMAppSrcPath, _baseUHEAppSrcPath);

                if (OperatingSystem.IsLinux())
                {
                    runner.TestDACOnLinux();
                }
                else if (OperatingSystem.IsWindows())
                {
                    runner.TestDACOnWindows();
                }
                else
                {
                    throw new Exception($"Unsupported Operating System");
                }
            }
            return 0;
        }
    }
}
