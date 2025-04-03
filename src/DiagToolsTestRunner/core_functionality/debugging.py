'''Debug target with native debugger
'''

import os
from tempfile import TemporaryDirectory

from core_functionality import common
from core_functionality.cli import CommandInvoker

class CLIDebugger:
    '''Invoke CLI debugger like cdb and lldb.
    '''
    def __init__(self, debugger_path: str):
        if common.rid_os_name() == 'linux-musl':
            raise OSError('Debugging is not supported on Alpine Linux')

        self.__debugger_name = os.path.basename(debugger_path)
        assert self.__debugger_name.startswith('cdb') or self.__debugger_name.startswith('lldb')

        self.__debugger_path = debugger_path

    def __generate_debugging_script(self, script_path: str, debug_command_list: list[str]):
        '''Generate debug script with quit command.
        
        :param script_path: script path
        :param debug_command_list: debugging command sequence
        '''
        with open(script_path, 'w+', encoding='utf-8') as fp:
            for command in debug_command_list:
                fp.write(f'{command}\n')

            if self.__debugger_name.startswith('cdb'):
                fp.write('.detach\n')
                fp.write('qq\n')
            if self.__debugger_name.startswith('lldb'):
                fp.write('exit\n')

    def debug_dump(self,
                   dump_path: str,
                   debug_command_list: list[str],
                   cwd: str=None,
                   redirect_std_in: bool=False,
                   redirect_std_out_err: bool=False,
                   silent: bool=False):
        '''Debug dump.
        
        :param dump_path: dump path
        :param debug_command_list: debugging command sequence
        :param cwd: working directory
        :param redirect_std_in: whether to redirect stardard input
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        :return: CommandInvoker instance
        '''
        temp_dir = TemporaryDirectory()
        script_path = os.path.join(temp_dir.name, 'debugging-dump-script.txt')

        self.__generate_debugging_script(script_path, debug_command_list)

        args = [self.__debugger_path]
        if self.__debugger_name.startswith('cdb'):
            args.extend(
                [
                    '-cf', script_path,
                    '-z', dump_path
                ]
            )
        if self.__debugger_name.startswith('lldb'):
            args.extend(
                [
                    '-s', script_path,
                    '-c', dump_path,
                    '--batch'
                ]
            )

        with CommandInvoker(args,
                            cwd=cwd,
                            redirect_std_in=redirect_std_in,
                            redirect_std_out_err=redirect_std_out_err,
                            silent=silent
        ) as invoker:
            invoker.communicate()
            temp_dir.cleanup()
            return invoker

    def debug_process(self,
                      pid: str,
                      debug_command_list: list[str],
                      cwd: str=None,
                      redirect_std_in: bool=False,
                      redirect_std_out_err: bool=False,
                      silent: bool=False):
        '''Debug process.
        
        :param pid: process id
        :param debug_command_list: debugging command sequence
        :param cwd: working directory
        :param redirect_std_in: whether to redirect stardard input
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        :return: CommandInvoker instance
        '''
        temp_dir = TemporaryDirectory()
        script_path = os.path.join(temp_dir.name, 'debugging-process-script.txt')

        self.__generate_debugging_script(script_path, debug_command_list)

        args = [self.__debugger_path]
        if self.__debugger_name.startswith('cdb'):
            args.extend(
                [
                    '-cf', script_path,
                    '-p', pid,
                ]
            )
        if self.__debugger_name.startswith('lldb'):
            args.extend(
                [
                    '-s', script_path,
                    '-p', pid,
                    '--batch'
                ]
            )

        with CommandInvoker(args,
                            cwd=cwd,
                            redirect_std_in=redirect_std_in,
                            redirect_std_out_err=redirect_std_out_err,
                            silent=silent
        ) as invoker:
            invoker.communicate()
            temp_dir.cleanup()
            return invoker

    def debug_launchable(self,
                         launchable: str,
                         debug_command_list: list[str],
                         cwd: str=None,
                         redirect_std_in: bool=False,
                         redirect_std_out_err: bool=False,
                         silent: bool=False):
        '''Debug process.
        
        :param launchable: path to launchable file
        :param debug_command_list: debugging command sequence
        :param cwd: working directory
        :param redirect_std_in: whether to redirect stardard input
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        :return: CommandInvoker instance
        '''
        temp_dir = TemporaryDirectory()
        script_path = os.path.join(temp_dir.name, 'debugging-launchable-script.txt')

        self.__generate_debugging_script(script_path, debug_command_list)

        args = [self.__debugger_path]
        if self.__debugger_name.startswith('cdb'):
            args.extend(
                [
                    '-g', '-cf', script_path, launchable
                ]
            )
        if self.__debugger_name.startswith('lldb'):
            args.extend(
                [
                    '-s', script_path, '--batch', launchable
                ]
            )

        with CommandInvoker(args,
                            cwd=cwd,
                            redirect_std_in=redirect_std_in,
                            redirect_std_out_err=redirect_std_out_err,
                            silent=silent
        ) as invoker:
            invoker.communicate()
            temp_dir.cleanup()
            return invoker
