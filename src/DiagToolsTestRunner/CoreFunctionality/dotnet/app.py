'''DotNet app utilities
'''

import os
from xml.etree import ElementTree as ET

from CoreFunctionality.cli import CommandInvoker
from CoreFunctionality.dotnet.environment import DotNetEnvironment

class DotNetApp:
    '''Represent a .NET app(even isn't created).\
        Contains information of an app and implements related methods.

    '''
    def __init__(self, dotnet_env: DotNetEnvironment, root: str, template: str, name: str=None):
        '''
        :param dotnet_env: DotNetEnvironment instance
        :param root: .NET app root path
        :param template: .NET app template
        :param name: app name; if it's None, the app name is name of app root
        '''
        self.__root = root
        self.__template = template
        self.__dotnet_env = dotnet_env

        if name is None:
            self.__name = os.path.basename(root)
        else:
            self.__name = name


    @property
    def app_root(self):
        '''Get app root.
        '''
        return self.__root

    @property
    def app_template(self):
        '''Get app template.
        '''
        return self.__template

    @property
    def app_name(self):
        '''Get app name.
        '''
        return self.__name

    @property
    def dotnet_env(self):
        '''Get .NET environment.
        '''
        return self.__dotnet_env

    def get_project_file_path(self):
        '''Get project file of a .NET app.

        :return: project file of a .NET app
        '''
        return os.path.join(self.__root, f'{self.__name}.csproj')

    def get_target_framework(self):
        '''Get target framework of a .NET app.

        :return: target framework of a .NET app
        '''
        project_file_path = self.get_project_file_path()
        tree = ET.parse(project_file_path)
        root = tree.getroot()
        return root.find('PropertyGroup').find('TargetFramework').text

    def get_symbol_folder(self,
                          target_framework: str,
                          build_config: str,
                          target_rid: str,
                          output_folder: str=None):
        '''Get symbol folder of a .NET app.

        :param target_framework: target framework of a .NET app
        :param build_config: build configuration
        :param target_rid: target runtime
        :param output_folder: built binaries root
        :return: symbol folder of a .NET app
        '''
        assert build_config.lower() in ['debug', 'release']

        if output_folder is None:
            output_folder = os.path.join(self.__root, 'bin')

        return os.path.join(output_folder, build_config, target_framework, target_rid)

    def get_executable(self,
                       target_framework: str,
                       build_config: str,
                       target_rid: str,
                       output_folder: str=None):
        '''Get executable of a .NET app.

        :param target_framework: target framework of a .NET app
        :param build_config: build configuration
        :param target_rid: target runtime
        :param output_folder: built binaries root
        :return: executable of a .NET app
        '''
        symbol_folder = self.get_symbol_folder(
            target_framework,
            build_config,
            target_rid,
            output_folder
        )
        return os.path.join(
            symbol_folder,
            f'{self.__name}{DotNetEnvironment.get_executable_file_extension_by_rid(target_rid)}'
        )

    def get_native_symbol_folder(self,
                                 target_framework: str,
                                 build_config: str,
                                 target_rid: str,
                                 output_folder: str=None):
        '''Get nativeaot built symbol folder of a .NET app.

        :param target_framework: target framework of a .NET app
        :param build_config: build configuration
        :param target_rid: target runtime
        :param output_folder: built binaries root
        :return: nativeaot built symbol folder of a .NET app
        '''
        assert build_config.lower() in ['debug', 'release']

        if output_folder is None:
            output_folder = os.path.join(self.__root, 'bin')

        return os.path.join(output_folder, build_config, target_framework, target_rid, 'native')

    def get_native_executable(self,
                              target_framework: str,
                              build_config: str,
                              target_rid: str,
                              output_folder: str=None):
        '''Get nativeaot built executable of a .NET app.

        :param target_framework: target framework of a .NET app
        :param build_config: build configuration
        :param target_rid: target runtime
        :param output_folder: built binaries root
        :return: nativeaot built executable of a .NET app
        '''
        native_symbol_folder = self.get_native_symbol_folder(
            target_framework,
            build_config,
            target_rid,
            output_folder
        )
        return os.path.join(
            native_symbol_folder,
            f'{self.__name}{DotNetEnvironment.get_executable_file_extension_by_rid(target_rid)}'
        )

    def create(self, redirect_std_out_err: bool=True, silent: bool=False):
        '''Create a .NET app.

        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output

        :return: CommandInvoker instance
        '''
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

    def build(self,
              build_config: str,
              target_rid: str,
              redirect_std_out_err: bool=True,
              silent: bool=False):
        '''Build a .NET app.

        :param build_config: build configuration
        :param target_rid: target runtime
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output

        :return: CommandInvoker instance
        '''
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

    def publish(self,
                build_config: str,
                target_rid: str,
                redirect_std_out_err: bool=True,
                silent: bool=False):
        '''Publish a .NET app.

        :param build_config: build configuration
        :param target_rid: target runtime
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output

        :return: CommandInvoker instance
        '''
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
