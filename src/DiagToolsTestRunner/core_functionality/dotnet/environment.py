'''DotNet environment utilities
'''

import os

from core_functionality import common

class DotNetEnvironment:
    '''Basic information of .NET environment.
    '''
    def __init__(self,
                 dotnet_root: str,
                 sdk_full_version: str,
                 target_rid: str,
                 dotnet_tool_root: str=None):
        '''
        :param dotnet_root: .NET SDK root
        :param sdk_full_version: full version of a .NET SDK
        :param target_rid: rid
        :param dotnet_tool_root: .NET tool root
        '''
        self.__dotnet_root = dotnet_root
        self.__dotnet_executable = os.path.join(
            dotnet_root,
            f'dotnet{self.get_executable_file_extension_by_rid(target_rid)}')

        if dotnet_tool_root is None:
            self.__dotnet_tool_root = os.path.join(
                common.user_profile(),
                '.dotnet',
                'tools'
            )
        else:
            self.__dotnet_tool_root = dotnet_tool_root
        self.__target_rid = target_rid
        self.__sdk_full_version = sdk_full_version

        env_connector = ''
        match target_rid.split('-')[0]:
            case 'win': env_connector = ';'
            case 'osx' | 'linux': env_connector = ':'
            case _: raise ValueError(f'Unknown rid: {target_rid}')
        self.__env = os.environ.copy()
        self.__env['DOTNET_ROOT'] = self.dotnet_root
        self.__env['PATH'] = f'{self.dotnet_root}{env_connector}' + self.__env['PATH']
        if dotnet_tool_root is not None:
            self.__env['PATH'] = f'{self.dotnet_tool_root}{env_connector}' + self.__env['PATH']

    @property
    def dotnet_root(self):
        return self.__dotnet_root

    @property
    def dotnet_executable(self):
        return self.__dotnet_executable

    @property
    def dotnet_tool_root(self):
        return self.__dotnet_tool_root

    @property
    def target_rid(self):
        return self.__target_rid

    @property
    def sdk_full_version(self):
        return self.__sdk_full_version

    @property
    def environment_variables(self):
        return self.__env

    @property
    def compressed_file_extension(self):
        return self.get_compressed_file_extension_by_rid(self.__target_rid)

    @property
    def executable_file_extension(self):
        return self.get_executable_file_extension_by_rid(self.__target_rid)

    @property
    def dotnet_dump_generation_environment(self, dump_path: str):
        return self.generate_dotnet_dump_generation_environment(self.__env, dump_path)

    @property
    def dotnet_tracing_environment(self):
        return self.generate_dotnet_tracing_environment(self.__env)

    @property
    def dotnet_stress_log_environment(self):
        return self.generate_dotnet_stress_log_environment(self.__env)

    @staticmethod
    def get_compressed_file_extension_by_rid(target_rid: str):
        '''Get compressed file extension according to given rid.

        :param target_rid: rid
        :return: compressed file extension
        '''
        match target_rid.split('-')[0]:
            case 'win': return '.zip'
            case 'osx' | 'linux': return '.tar.gz'
            case _: raise ValueError(f'Unknown rid: {target_rid}')

    @staticmethod
    def get_executable_file_extension_by_rid(target_rid: str):
        '''Get executable file extension according to given rid.

        :param target_rid: rid
        :return: executable file extension
        '''
        match target_rid.split('-')[0]:
            case 'win': return '.exe'
            case 'osx' | 'linux': return ''
            case _: raise ValueError(f'Unknown rid: {target_rid}')

    @staticmethod
    def generate_dotnet_dump_generation_environment(base_env: dict, dump_path: str):
        '''Generate .NET environment that enable dump generation.

        :param base_env: basic environment variables
        :return: .NET environment that enable dump generation
        '''
        _env = base_env.copy()
        _env['DOTNET_DbgEnableMiniDump'] = '1'
        _env['DOTNET_DbgMiniDumpType'] = '4'
        _env['DOTNET_DbgMiniDumpName'] = dump_path
        return _env

    @staticmethod
    def generate_dotnet_tracing_environment(base_env: dict):
        '''Generate .NET environment that enable tracing.

        :param base_env: basic environment variables
        :return: .NET environment that enable tracing
        '''
        _env = base_env.copy()
        _env['DOTNET_PerfMapEnabled'] = '1'
        _env['DOTNET_EnableEventLog'] = '1'
        return _env

    @staticmethod
    def generate_dotnet_stress_log_environment(base_env: dict):
        '''Generate .NET environment that enable stress log.

        :param base_env: basic environment variables
        :return: .NET environment that enable stress log
        '''
        _env = base_env.copy()
        _env['DOTNET_StressLog'] = '1'
        _env['DOTNET_StressLogLevel'] = '10'
        _env['DOTNET_TotalStressLogSize'] = '8196'
        return _env

    def generate_dotnet_environment_activation_script(self, script_path_without_extension: str):
        '''Generate .NET environment activation script.

        :param script_path_without_extension: script path without file extension
        '''
        script_path = script_path_without_extension
        script_content = ''
        if self.target_rid.startswith('win'):
            script_path += '.ps1'
            script_content += f'$Env:DOTNET_ROOT=\"{self.dotnet_root}\"\n'
            script_content += f'$Env:Path+=\";{self.dotnet_root}\"\n'
            if self.dotnet_tool_root is not None:
                script_content += f'$Env:Path+=\";{self.dotnet_tool_root}\"\n'

        elif self.target_rid.startswith('linux') or self.target_rid.startswith('osx'):
            script_path += '.sh'
            script_content += f'export DOTNET_ROOT={self.dotnet_root}'
            script_content += f'export PATH=$PATH:{self.dotnet_root}'
            if self.dotnet_tool_root is not None:
                script_content += f'export PATH=$PATH:{self.dotnet_tool_root}'

        with open(script_path, 'w+', encoding='utf-8') as fp:
            fp.write(script_content)
