import os
from typing import override

from easycli import *

from utility.logger import ProgressRecorder 
from utility.logger import AppLogger
from core_functionality.configuration.diagnostic_tools.diagnostic_tools_test_configuration import DiagToolTestCommandConfiguration
from core_functionality.job.diagnostic_tools import initialize, test_dotnet_counters


class DiagToolTestCommandSetting(CommandSetting):
    def __init__(self):
        super().__init__()

    @property
    @CommandSetting.command_option('-c', '--configuration')
    def configuration_path(self):
        return
    

class DiagToolTestCommandRunner(DiagToolTestCommandSetting):
    def __init__(self):
        super().__init__()

    @override
    def execute(self, setting: DiagToolTestCommandSetting):
        test_configuration = DiagToolTestCommandConfiguration(setting.configuration_path)

        logger_name = self.__class__.__name__
        logger_path = os.path.join(test_configuration.test_result_folder, f'{logger_name}.log')
        logger = AppLogger(logger_name, logger_path)
        
        # initialize
        '''
        logger.info('create testbed and test result folder')
        os.makedirs(test_configuration.test_result_folder, exist_ok=True)

        initialization_command_invoker_recording_path = os.path.join(
            test_configuration.test_result_folder, 
            'initialization.txt'
        )
        initialization_processer = ProgressRecorder(initialization_command_invoker_recording_path)
        
        logger.info(f'install .NET SDK, see {initialization_command_invoker_recording_path} for details')
        initialization_processer.advance(initialize.install_dot_Net_SDK(test_configuration))

        logger.info(f'install diagnotic tools, see {initialization_command_invoker_recording_path} for details')
        initialization_processer.advance(initialize.install_diagnostic_tools(test_configuration))

        logger.info(f'prepare target apps, see {initialization_command_invoker_recording_path} for details')
        initialization_processer.advance(initialize.prepare_target_apps(test_configuration))
        '''

        # 
        dotnet_counters_command_invoker_recording_path = os.path.join(
            test_configuration.test_result_folder, 
            'dotnet-counters.txt'
        )
        dotnet_counters_test_processer = ProgressRecorder(dotnet_counters_command_invoker_recording_path)
        logger.info(f'Test dotnet-counters, see {dotnet_counters_command_invoker_recording_path} for details')
        dotnet_counters_test_processer.advance(test_dotnet_counters.test_dotnet_counters(test_configuration))
        return