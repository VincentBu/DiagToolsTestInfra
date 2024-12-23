import os
import xml.etree.ElementTree as ET
from typing import Union

from utility.cli import CommandInvoker
from utility.system_info import SysInfo
from utility.DotNet import dotnet

class DotNetApp():
    def __init__(self,
                 dotNet_root: str, 
                 app_template: str,
                 app_root: str):
        self.__dotNet_root = dotNet_root
        self.__app_template = app_template
        self.__app_root = app_root
        self.__app_name = os.path.basename(app_root)

        self.__valid_build_config = ['Debug', 'Release']

    @property
    def app_root(self):
        return self.__app_root

    @property
    def app_template(self):
        return self.__app_template
    
    @property
    def app_name(self):
        return self.__app_name
    
    def get_target_framework(self) -> Union[str, Exception]:
        try:
            project_file =os.path.join(self.__app_root, f'{self.__app_name}.csproj')
            tree = ET.parse(project_file)
            return tree.getroot().find('PropertyGroup').find('TargetFramework').text
        except Exception as ex:
            return Exception(f'Fail to get target framework for {self.__app_root}: {ex}')

    def create_new_app(self) -> CommandInvoker:
        options = ['new', self.__app_template, '-o', self.__app_root, '--force']
        invoker = dotnet.run_dot_net_command(
            self.__dotNet_root, options,
            wait_for_exit=True,
            silent_run=False
        )
        return invoker

    def build_app(self, build_config: str) -> CommandInvoker:
        if build_config not in self.__valid_build_config:
            return Exception(f'Unknown build config for app building: {build_config}')
        
        options = ['build', '-c', build_config]
        invoker = dotnet.run_dot_net_command(
            self.__dotNet_root,
            options,
            wait_for_exit=True,
            silent_run=False,
            cwd=self.__app_root
        )
        return invoker

    def publish_app(self, build_config: str) -> CommandInvoker:
        if build_config not in self.__valid_build_config:
            return Exception(f'Unknown build config for app publishing: {build_config}')
        
        options = ['publish', '-c', build_config]
        invoker = dotnet.run_dot_net_command(
            self.__dotNet_root,
            options,
            wait_for_exit=True,
            silent_run=False,
            cwd=self.__app_root
        )
        return invoker
    
    def get_app_IL(self, build_config: str) -> Union[str, Exception]:
        if build_config not in self.__valid_build_config:
            return Exception(f'Unknown build config for {self.__app_name} IL searching: {build_config}')
        
        target_framework = self.get_target_framework()
        app_IL = os.path.join(self.__app_root, 'bin', build_config, target_framework, f'{self.__app_name}.dll')
        if not os.path.exists(app_IL):
            return Exception(f'Fail to find IL file for {self.__app_name}')
        else:
            return app_IL
        
    def get_app_symbol_folder(self, build_config: str) -> Union[str, Exception]:
        if build_config not in self.__valid_build_config:
            return Exception(f'Unknown build config for {self.__app_name} symbol folder searching: {build_config}')
        
        app_IL = self.get_app_IL(build_config)

        if isinstance(app_IL, Exception):
            return app_IL
        
        return os.path.dirname(app_IL)

    def get_app_executable(self, build_config: str) -> Union[str, Exception]:
        if build_config not in self.__valid_build_config:
            return Exception(f'Unknown build config for {self.__app_name} executable searching: {build_config}')
        
        app_symbol_root = self.get_app_symbol_folder(build_config)

        if isinstance(app_symbol_root, Exception):
            return app_symbol_root
        
        return os.path.join(app_symbol_root, f'{self.__app_name}{SysInfo.ExecutableExtension}')