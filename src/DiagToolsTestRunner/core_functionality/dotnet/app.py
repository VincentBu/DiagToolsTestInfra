'''DotNet app utilities
'''

import os

from core_functionality.cli import CommandInvoker
from core_functionality.dotnet.environment import DotNetEnvironment

class DotNetApp:
    '''Represent a .NET app(even isn't created).\
        Contains information of an app and implements related methods.

    '''
    def __init__(self,
                 dotnet_env: DotNetEnvironment,
                 root: str,
                 template: str,
                 build_config: str='Debug',
                 output_folder: str=None,
                 name: str=None):
        '''
        :param dotnet_env: DotNetEnvironment instance
        :param root: .NET app root path
        :param template: .NET app template
        :param build_config: build configuration
        :param name: app name; if it's None, the app name is name of app root
        '''
        assert build_config.lower() in ['debug', 'release']
        self.__root = root
        self.__template = template
        self.__build_config = build_config
        self.__target_rid = dotnet_env.target_rid
        self.__dotnet_env = dotnet_env
        self.__target_framework = f'net{dotnet_env.sdk_full_version[:3]}'

        if output_folder is None:
            self.__output_folder = os.path.join(self.__root, 'bin')
        else:
            self.__output_folder = output_folder

        if name is None:
            self.__name = os.path.basename(root)
        else:
            self.__name = name

        self.__project_file = os.path.join(self.__root, f'{self.__name}.csproj')
        self.__symbol_folder = os.path.join(
            self.__output_folder,
            self.__build_config,
            self.__target_framework,
            self.__target_rid
        )
        self.__executable = os.path.join(
            self.__symbol_folder,
            f'{self.__name}{self.__dotnet_env.executable_file_extension}'
        )

        self.__native_symbol_folder = os.path.join(self.__symbol_folder, 'native')
        self.__native_executable = os.path.join(
            self.__native_symbol_folder,
            f'{self.__name}{self.__dotnet_env.executable_file_extension}'
        )

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
    def target_framework(self):
        '''Get target framework.
        '''
        return self.__target_framework

    @property
    def dotnet_env(self):
        '''Get .NET environment.
        '''
        return self.__dotnet_env

    @property
    def project_file_path(self):
        '''Get project file of a .NET app.
        '''
        return self.__project_file

    @property
    def symbol_folder(self):
        '''Get symbol folder of a .NET app.
        '''
        return self.__symbol_folder

    @property
    def executable(self):
        '''Get executable of a .NET app.
        '''
        return self.__executable

    @property
    def native_symbol_folder(self):
        '''Get nativeaot built symbol folder of a .NET app.
        '''
        return self.__native_symbol_folder

    @property
    def native_executable(self):
        '''Get nativeaot built executable of a .NET app.
        '''
        return self.__native_executable

    def create(self, redirect_std_out_err: bool=True, silent: bool=False):
        '''Build a .NET app.
        
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
        ) as invoker:
            invoker.communicate()
            return invoker

    def build(self, redirect_std_out_err: bool=True, silent: bool=False):
        '''Build a .NET app.

        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output

        :return: CommandInvoker instance
        '''
        args = [
            self.__dotnet_env.dotnet_executable, 'build',
            '-r', self.__target_rid,
            '-c', self.__build_config
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

    def publish(self, redirect_std_out_err: bool=True, silent: bool=False):
        '''Publish a .NET app.

        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output

        :return: CommandInvoker instance
        '''
        args = [
            self.__dotnet_env.dotnet_executable, 'publish',
            '-r', self.__target_rid,
            '-c', self.__build_config
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
