'''DotNet tool utilities
'''

import os
import glob

from CoreFunctionality.cli import CommandInvoker
from CoreFunctionality.dotnet.environment import DotNetEnvironment

class DotNetTool:
    '''Contains basic information of .NET tools.
    '''
    def __init__(self, dotnet_env: DotNetEnvironment):
        '''
        :param dotnet_env: a DotNetEnvironment instance
        '''
        self.__dotnet_env = dotnet_env

    def install_dotnet_tool(self,
                            name: str,
                            version: str,
                            feed: str,
                            config_file_path: str=None,
                            redirect_std_out_err: bool=True,
                            silent: bool=False):
        '''Install .NET tool.

        :param name: tool name
        :param version: tool version
        :param feed: tool feed
        :param config_file_path: path of config file
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        '''
        tool_root = self.__dotnet_env.dotnet_tool_root
        assert tool_root is not None
        args = [
            self.__dotnet_env.dotnet_executable, 'tool', 'install', name,
            '--tool-path', tool_root, 
            '--version', version,
            '--add-source', feed]
        if config_file_path is not None:
            args.extend(['--configfile', config_file_path])
        with CommandInvoker(args,
                            env=self.__dotnet_env.environment_variables,
                            redirect_std_out_err=redirect_std_out_err,
                            silent=silent
        ) as p:
            p.communicate()
            return p

    def get_dotnet_tool_il(self, name: str, version: str):
        '''GEt IL(.dll) of .NET tool.
        
        :param name: tool name
        :param version: tool version
        '''
        tool_root = self.__dotnet_env.dotnet_tool_root
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

        return tool_il_candidates[0]
