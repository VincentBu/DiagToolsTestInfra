'''DotNet tool utilities
'''

import os
import glob

from core_functionality.cli import CommandInvoker
from core_functionality.dotnet.environment import DotNetEnvironment

class DotNetDiagnosticTool:
    '''Contains basic information of .NET tools.
    '''
    def __init__(self,
                 name: str,
                 dotnet_env: DotNetEnvironment,
                 version: str=None):
        '''
        :param name: diagnostic tool name
        :param dotnet_env: a DotNetEnvironment instance
        :param version: diagnostic tool version
        '''
        self.__name = name
        self.__dotnet_env = dotnet_env
        self.__version = version

    @property
    def dotnet_env(self):
        '''Get .NET environment.
        '''
        return self.__dotnet_env

    @property
    def tool_name(self):
        '''Get diagnostic tool name.
        '''
        return self.__name

    @property
    def tool_version(self):
        '''Get diagnostic tool version.
        '''
        return self.__version

    def install_tool(self,
                     feed: str=None,
                     config_file_path: str=None,
                     redirect_std_out_err: bool=True,
                     silent: bool=False):
        '''Install .NET tool.
        
        :param feed: diagnostic tool feed
        :param config_file_path: tool installation config file
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        '''
        tool_root = self.__dotnet_env.dotnet_tool_root
        assert tool_root is not None
        args = [
            self.__dotnet_env.dotnet_executable, 'tool', 'install', self.__name,
            '--tool-path', tool_root
        ]

        if self.__version is not None:
            args.extend(['--version', self.__version])
        if feed is not None:
            args.extend(['--add-source', feed])
        if config_file_path is not None:
            args.extend(['--configfile', config_file_path])

        with CommandInvoker(
            args,
            env=self.__dotnet_env.environment_variables,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as invoker:
            invoker.communicate()
            return invoker

    def get_tool_il(self):
        '''Get il(.dll) of tool
        
        :return: il of tool
        '''
        tool_root = self.__dotnet_env.dotnet_tool_root
        assert tool_root is not None
        tool_il_template = os.path.join(
            tool_root,
            '.store',
            self.__name,
            self.__version,
            self.__name,
            self.__version,
            'tools',
            'net*',
            'any',
            f'{self.__name}.dll'
        )

        tool_il_candidates = glob.glob(tool_il_template)
        if len(tool_il_candidates) < 1:
            raise FileNotFoundError(
                f'Fail to find dll file for {self.__name} {self.__version} in {tool_root}')

        return tool_il_candidates[0]

    def invoke_diagnostic_tool(self,
                               optional_args: list[str]=None,
                               cwd: str=None,
                               redirect_std_in: bool=False,
                               redirect_std_out_err: bool=False,
                               silent: bool=False):
        '''Invoke dotnet-trace by running "dotnet dotnet-trace.dll <optional args>".

        :param optional_args: optinal arguments
        :param cwd: working directory
        :param redirect_std_in: whether to redirect stardard input
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        '''
        tool_il = self.get_tool_il()
        assert os.path.exists(tool_il)

        args = [
            self.__dotnet_env.dotnet_executable,
            tool_il
        ]

        args.extend(optional_args)

        with CommandInvoker(
            args,
            cwd=cwd,
            env=self.__dotnet_env.environment_variables,
            redirect_std_in=redirect_std_in,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as p:
            p.communicate()
            return p
