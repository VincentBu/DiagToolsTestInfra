import os
from xml.etree import ElementTree as ET

from CoreFunctionality.cli import CommandInvoker
from CoreFunctionality.DotNetEnvironment import DotNetEnvironment 

class DotNetApp:
    def __init__(self, dotnet_env: DotNetEnvironment, root: str, template: str, name: str=None):
        self.__root = root
        self.__template = template
        self.__dotnet_env = dotnet_env

        if name is None:
            self.__name = os.path.basename(root)
        else:
            self.__name = name

    def get_project_file_path(self):
        return os.path.join(self.__root, f'{self.__name}.csproj')
    
    def get_target_framework(self):
        project_file_path = self.get_project_file_path()
        tree = ET.parse(project_file_path)
        root = tree.getroot()
        return root.find('PropertyGroup').find('TargetFramework').text
    
    def get_symbol_folder(self, target_framework: str, build_config: str, target_rid: str, output_folder: str=None):
        if output_folder is None:
            output_folder = os.path.join(self.__root, 'bin')

        return os.path.join(output_folder, build_config, target_framework, target_rid)
    
    def get_executable(self, target_framework: str, build_config: str, target_rid: str, output_folder: str=None):
        symbol_folder = self.get_symbol_folder(target_framework, build_config, target_rid, output_folder)
        return os.path.join(
            symbol_folder,
            f'{self.__name}{DotNetEnvironment.get_executable_file_extension_by_rid(target_rid)}'
        )
    
    def get_native_symbol_folder(self, target_framework: str, build_config: str, target_rid: str, output_folder: str=None):
        if output_folder is None:
            output_folder = os.path.join(self.__root, 'bin')

        return os.path.join(output_folder, build_config, target_framework, target_rid, 'native')
    
    def get_native_executable(self, target_framework: str, build_config: str, target_rid: str, output_folder: str=None):
        native_symbol_folder = self.get_native_symbol_folder(target_framework, build_config, target_rid, output_folder)
        return os.path.join(
            native_symbol_folder,
            f'{self.__name}{DotNetEnvironment.get_executable_file_extension_by_rid(target_rid)}'
        )
    
    def create(self, redirect_std_out_err: bool=True, silent: bool=False):
        args = [
            self.__dotnet_env.dotnet_executable, 'new', self.__template,
            '-o', self.__root,
            '-n', self.__name,
            '--force'
        ]
        with CommandInvoker(
            args,
            env=self.__dotnet_env.environment_variables,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as p:
            p.communicate()
            return p
    
    def build(self, build_config: str, target_rid: str, redirect_std_out_err: bool=True, silent: bool=False):
        args = [
            self.__dotnet_env.dotnet_executable, 'build',
            '-r', target_rid,
            '-c', build_config
        ]
        with CommandInvoker(
            args,
            env=self.__dotnet_env.environment_variables,
            cwd=self.__root,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as p:
            p.communicate()
            return p
        
    def publish(self, build_config: str, target_rid: str, redirect_std_out_err: bool=True, silent: bool=False):
        args = [
            self.__dotnet_env.dotnet_executable, 'publish',
            '-r', target_rid,
            '-c', build_config
        ]
        with CommandInvoker(
            args,
            env=self.__dotnet_env.environment_variables,
            cwd=self.__root,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as p:
            p.communicate()
            return p