import os
import glob
from urllib import request
from http.client import HTTPResponse

from CoreFunctionality import common
from CoreFunctionality.cli import CommandInvoker


class DotNetEnvironment:
    def __init__(self, dotnet_root: str, sdk_full_version: str, target_rid: str, dotnet_tool_root: str=None):
        
        self.__dotnet_root = dotnet_root
        self.__dotnet_executable = os.path.join(
            dotnet_root,
            f'dotnet{self.get_executable_file_extension_by_rid(target_rid)}')
        self.__dotnet_tool_root = dotnet_tool_root
        self.__target_rid = target_rid
        self.__sdk_full_version = sdk_full_version

        env_connector = self.get_environment_variable_connector()
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
    def environment_variable_connector(self):
        self.get_environment_variable_connector_by_rid(self.__target_rid)

    @property
    def compressed_file_extension(self):
        self.get_compressed_file_extension_by_rid(self.__target_rid)

    @property
    def executable_file_extension(self):
        self.get_executable_file_extension_by_rid(self.__target_rid)

    @staticmethod
    def get_environment_variable_connector_by_rid(target_rid: str):
        match target_rid.split('-')[0]:
            case 'win': return ';'
            case 'osx' | 'linux': return ':'
            case _: raise Exception(f'Unknown rid: {target_rid}')

    @staticmethod
    def get_compressed_file_extension_by_rid(target_rid: str):
        match target_rid.split('-')[0]:
            case 'win': return '.zip'
            case 'osx' | 'linux': return '.tar.gz'
            case _: raise Exception(f'Unknown rid: {target_rid}')

    @staticmethod
    def get_executable_file_extension_by_rid(target_rid: str):
        match target_rid.split('-')[0]:
            case 'win': return '.exe'
            case 'osx' | 'linux': return ''
            case _: raise Exception(f'Unknown rid: {target_rid}')

    def install_DotNet_SDK(self):
        def __generate_DotNet_SDK_download_link(sdk_full_version: str, target_rid: str):
            AzureFeedList = [
                'https://builds.dotnet.microsoft.com/dotnet',
                'https://ci.dot.net/public'
            ]

            for feed in AzureFeedList:
                product_version_query_url = f'{feed}/Sdk/{sdk_full_version}/sdk-productVersion.txt'
                try:
                    response: HTTPResponse = request.urlopen(product_version_query_url)
                    product_version = response.read().decode().replace('\n', '').replace('\r', '')
                    return f'{feed}/Sdk/{sdk_full_version}/dotnet-sdk-{product_version}-{target_rid}{ self.compressed_file_extension}'
                except Exception:
                    continue

            raise Exception(f'Fail to generate download link for {sdk_full_version}')
        
        sdk_download_url = __generate_DotNet_SDK_download_link(self.sdk_full_version, self.target_rid)

        os.makedirs(self.dotnet_root, exist_ok=True)
        sdk_download_path = os.path.join(self.dotnet_root, os.path.basename(sdk_download_url))
        common.http_download(sdk_download_path, sdk_download_url)

        if sdk_download_path.endswith('.tar.gz'):
            common.extract_tar_gz(sdk_download_path, self.dotnet_root)
        elif sdk_download_path.endswith('.zip'):
            common.extract_zip(sdk_download_path, self.dotnet_root)
        else:
            raise Exception(f'unknown compressed file {sdk_download_path}')
        
        os.remove(sdk_download_path)

    def generate_DotNet_environment_activation_script(self, script_path_without_extension: str):
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
        
        with open(script_path, 'w+') as fp:
            fp.write(script_content)

    def generate_DotNet_dump_generation_environment(self, dump_path: str):
        _env = self.__env.copy()
        _env['DOTNET_DbgEnableMiniDump'] = '1'
        _env['DOTNET_DbgMiniDumpType'] = '4'
        _env['DOTNET_DbgMiniDumpName'] = dump_path
        return _env
    
    def generate_DotNet_tracing_environment(self):
        _env = self.__env.copy()
        _env['DOTNET_PerfMapEnabled'] = '1'
        _env['DOTNET_EnableEventLog'] = '1'
        return _env
    
    def generate_DotNet_stress_log_environment(self):
        _env = self.__env.copy()
        _env['DOTNET_StressLog'] = '1'
        _env['DOTNET_StressLogLevel'] = '10'
        _env['DOTNET_TotalStressLogSize'] = '8196'
        return _env
    
    def install_DotNet_tool(self,
                            name: str,
                            version: str,
                            feed: str,
                            config_file_path: str=None,
                            redirect_std_out_err: bool=True,
                            silent: bool=False):
        assert self.__dotnet_tool_root is not None
        args = [
            self.__dotnet_executable, 'tool', 'install', name, 
            '--tool-path', self.__dotnet_tool_root, 
            '--version', version,
            '--add-source', feed]
        if config_file_path is not None:
            args.extend(['--configfile', config_file_path])
        with CommandInvoker(args, env=self.__env, redirect_std_out_err=redirect_std_out_err, silent=silent) as p:
            p.communicate()
            return p
        
    def get_DotNet_tool_IL(self, name: str, version: str):
        assert self.__dotnet_tool_root is not None
        tool_IL_template = os.path.join(
            self.__dotnet_tool_root, '.store', name, version, name, version, 'tools', 'net*', 'any', f'{name}.dll'
        )

        tool_IL_candidates = glob.glob(tool_IL_template)
        if len(tool_IL_template) < 1:
            raise Exception(f'Fail to find dll file for {name} {version} in {self.__dotnet_tool_root}')
        
        return tool_IL_candidates[0]
