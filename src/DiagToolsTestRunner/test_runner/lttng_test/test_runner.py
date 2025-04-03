'''Implement test runner
'''

import os
from typing import Callable, Generator, Any

from core_functionality.cli import CommandInvoker, command_sequence_runner
from test_assets import gcperfsim as gcperfsim_assets
from test_runner.lttng_test.config import LTTngTestConfig


class LTTngTestRunner:
    '''Run LTTng test.
    '''
    def __init__(self,
                 config: LTTngTestConfig,
                 init_logger_name: str=None,
                 ignore_error: bool=False):
        '''
        :param config: a LTTngTestConfig instance
        '''
        self.__config = config
        if init_logger_name is None:
            init_logger_path = os.path.join(
                self.__config.TestResultFolder,
                f'init-{self.__config.DotNetEnvironment.sdk_full_version}.log'
            )
        else:
            init_logger_path = os.path.join(
                self.__config.TestResultFolder,
                f'init-{init_logger_name}.log'
            )
        command_sequence_runner(
            init_logger_path, self.__initialize_test(), ignore_error=ignore_error)

    def __initialize_test(self):
        '''Initialize environment for testing.

        :param config: a LTTngTestConfig instance
        :return: enumerable CommandInvoker
        '''
        # install sdk
        yield self.__config.Installer.install_dotnet_sdk()

        # create gcperfsim
        yield self.__config.GCPerfsim.create()

        # replace source code file
        target_src_file_path = os.path.join(
            self.__config.GCPerfsim.app_root,
            'Program.cs'
        )
        with open(gcperfsim_assets.gcperfsim_src_path, mode='r', encoding='utf-8') as reader:
            with open(target_src_file_path, mode='w', encoding='utf-8') as writer:
                writer.write(reader.read())

        # builf gcperfsim
        yield self.__config.GCPerfsim.build()

    def run_test_with_gcperfsim(self,
                                test: Callable[
                                    [LTTngTestConfig], Generator[CommandInvoker, Any, None]],
                                logger_name: str=None,
                                ignore_error: bool=False):
        '''Start gcperfsim for testing.
        
        :param test: a callable takes LTTngTestConfig as parameter and return a generator
        :param logger_name: logger name
        :param ignore_error: whether to ignore error 
        '''
        if logger_name is None:
            logger_path = os.path.join(
                self.__config.TestResultFolder,
                f'{test.__name__}-{self.__config.DotNetEnvironment.sdk_full_version}.log'
            )
        else:
            logger_path = os.path.join(
                self.__config.TestResultFolder,
                f'init-{logger_name}.log'
            )

        gcperfsim_run_args = [self.__config.GCPerfsim.executable]
        with CommandInvoker(
            gcperfsim_run_args,
            cwd=self.__config.TestResultFolder,
            env=self.__config.DotNetEnvironment.dotnet_tracing_environment
        ) as invoker:
            command_sequence_runner(
                logger_path, test(self.__config), ignore_error=ignore_error)
            invoker.kill()
