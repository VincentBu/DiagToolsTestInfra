'''Implement lttng test command runner
'''

from eazycli import add_argument, CommandLineArguments, CommandRunner

from test_runner.lttng_test.config import generate_lttng_test_config
from test_runner.lttng_test.test_runner import LTTngTestRunner
from test_runner.lttng_test.lttng_test import test_lttng

class TestLTTngCommandLineArguments(CommandLineArguments):
    '''Command line arguments for test-lttng command.
    '''
    @add_argument('-c', '--configuration-path')
    def configuration_path(self):
        '''Get configuration path from command line.
        '''

class TestLTTngCommandRunner(CommandRunner, TestLTTngCommandLineArguments):
    '''Implement runner for test-lttng command.
    '''
    def execute(self, command_line_arguments: TestLTTngCommandLineArguments):
        '''Implement execute method.

        :param cli_args: a TestLTTngCommandLineArgument
        '''
        lttng_test_config_list = generate_lttng_test_config(
            command_line_arguments.configuration_path)
        for lttng_test_config in lttng_test_config_list:
            runner = LTTngTestRunner(lttng_test_config)
            runner.run_test_with_gcperfsim(test_lttng)
