'''DotNet tool utilities
'''

import os
import glob

from CoreFunctionality.cli import CommandInvoker
from CoreFunctionality.dotnet.environment import DotNetEnvironment

class DotNetDiagnosticTool:
    '''Contains basic information of .NET tools.
    '''
    def __init__(self,
                 name: str,
                 dotnet_env: DotNetEnvironment,
                 version: str=None,
                 feed: str=None,
                 config_file_path: str=None):
        '''
        :param dotnet_env: a DotNetEnvironment instance
        '''
        self.__name = name
        self.__dotnet_env = dotnet_env
        self.__version = version

        # install tool
        tool_root = self.__dotnet_env.dotnet_tool_root
        assert tool_root is not None
        args = [
            self.__dotnet_env.dotnet_executable, 'tool', 'install', name,
            '--tool-path', tool_root
        ]

        if version is not None:
            args.extend(['--version', version])
        if feed is not None:
            args.extend(['--add-source', feed])
        if config_file_path is not None:
            args.extend(['--configfile', config_file_path])

        with CommandInvoker(args,
                            env=self.__dotnet_env.environment_variables,
                            redirect_std_out_err=True,
                            silent=True
        ) as p:
            p.communicate()

        # get tool il
        assert tool_root is not None
        tool_il_template = os.path.join(
            tool_root,
            '.store',
            name,
            version,
            name,
            version,
            'tools',
            'net*',
            'any',
            f'{name}.dll'
        )

        tool_il_candidates = glob.glob(tool_il_template)
        if len(tool_il_candidates) < 1:
            raise FileNotFoundError(
                f'Fail to find dll file for {name} {version} in {tool_root}')

        self.__tool_il = tool_il_candidates[0]

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
        assert os.path.exists(self.__tool_il)

        args = [
            self.__dotnet_env.dotnet_executable,
            self.__tool_il
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
