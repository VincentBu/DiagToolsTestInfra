'''DotNet SDK and runtime installer
'''

import os

from core_functionality import common
from core_functionality.cli import CommandInvoker
from core_functionality.dotnet.environment import DotNetEnvironment

class DotNetInstaller:
    '''Install .NET SDK and runtime.
    '''
    def __init__(self, script_root: str, dotnet_env: DotNetEnvironment):
        '''
        :param dotnet_env: DotNetEnvironment instance
        :param script_path: script path
        '''
        self.__dotnet_env = dotnet_env
        self.__shell_engine = ''

        self.__script_download_link = ''
        if common.rid_os_name().startswith('linux') or common.rid_os_name().startswith('osx'):
            self.__script_download_link = \
                'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh'
            self.__shell_engine = '/bin/bash'
        elif common.rid_os_name().startswith('win'):
            self.__script_download_link = \
                'https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1'
            self.__shell_engine = 'powershell.exe'
        else:
            raise OSError(f'Unsupported OS: {common.rid_os_name()}')

        self.__script_path = os.path.join(
            script_root,
            os.path.basename(self.__script_download_link))
        common.http_download(self.__script_path, self.__script_download_link)
        if common.rid_os_name().startswith('linux') or common.rid_os_name().startswith('osx'):
            enable_execute_args = ['chmod', '+x', self.__script_path]
            with CommandInvoker(enable_execute_args, os.environ, silent=False) as ci:
                ci.communicate()

    def install_dotnet_sdk(self):
        '''Install .NET SDK according to given DotNetEnvironment instance.
        
        '''
        args = [
            self.__shell_engine, self.__script_path,
            '-i', self.__dotnet_env.dotnet_root,
            '--version', self.__dotnet_env.sdk_full_version
        ]
        architecture = self.__dotnet_env.target_rid.split('-')[-1]
        if architecture != common.rid_machine_name():
            args.extend(['-Architecture', architecture])

        with CommandInvoker(args, silent=False) as invoker:
            invoker.communicate()
            return invoker
