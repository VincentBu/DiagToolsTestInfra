'''Implememt command invoking functionality
'''

import os
from tempfile import TemporaryDirectory
from threading import Thread
from subprocess import Popen, PIPE, SubprocessError
from typing import Generator, Any


class CommandInvoker(Popen):
    '''CommandInvoker inherit subprocess.Popen and implement live output.
    '''
    def __init__(self,
                 args: list[str],
                 cwd=None,
                 env=None,
                 redirect_std_in=True,
                 redirect_std_out_err=True,
                 silent=True):
        '''
        :param args: command in string list format
        :param cwd: working directory
        :param env: environment variables
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        '''
        self.__redirect_std_out_err = redirect_std_out_err
        self.__silent: bool = silent

        self.__command = ' '.join(args)

        self.__temp_dir = TemporaryDirectory()
        self.__std_out_file = os.path.join(self.__temp_dir.name, 'out.txt')
        self.__std_err_file = os.path.join(self.__temp_dir.name, 'err.txt')
        self.__stdout_write_stream = open(
            self.__std_out_file, mode='w+', encoding='utf-8'
        ) if self.__redirect_std_out_err else None
        self.__stderr_write_stream = open(
            self.__std_err_file, mode='w+', encoding='utf-8'
        ) if self.__redirect_std_out_err else None

        self.__stdout: str = ''
        self.__stderr: str = ''

        print(f'run command: {self.__command}')
        try:
            stdin = PIPE if redirect_std_in else None
            super().__init__(args,
                            bufsize=1,
                            cwd=cwd,
                            env=env,
                            stdin=stdin,
                            stdout=self.__stdout_write_stream,
                            stderr=self.__stderr_write_stream,
                            universal_newlines=True)
        except Exception:
            self.__stdout_write_stream.close()
            self.__stdout_write_stream.close()
            self.__temp_dir.cleanup()
            raise

        if redirect_std_out_err:
            self.__stdout_reader = Thread(target=self.__stdout_pipe_consumer)
            self.__stdout_reader.start()
            self.__stderr_reader = Thread(target=self.__stderr_pipe_consumer)
            self.__stderr_reader.start()

    def __stdout_pipe_consumer(self):
        try:
            with open(self.__std_out_file, mode='r+', encoding='utf-8') as stdout_read_stream:
                while self.returncode is None:
                    line = stdout_read_stream.readline()
                    if line.strip() in [None, '', '\n', '\r', '\r\n']:
                        continue
                    self.__stdout += f'{line}\n'
                    if not self.__silent:
                        print(f'    {line}')
        except Exception:
            self.__stdout_write_stream.close()
            self.__stdout_write_stream.close()
            self.__temp_dir.cleanup()
            raise

    def __stderr_pipe_consumer(self):
        try:
            with open(self.__std_err_file, mode='r+', encoding='utf-8') as stderr_read_stream:
                while self.returncode is None:
                    line = stderr_read_stream.readline()
                    if line.strip() in [None, '', '\n', '\r', '\r\n']:
                        continue
                    self.__stderr += f'{line}\n'
                    if not self.__silent:
                        print(f'    {line}')
        except Exception:
            self.__stdout_write_stream.close()
            self.__stdout_write_stream.close()
            self.__temp_dir.cleanup()
            raise

    def __exit__(self, exc_type, value, traceback):
        if self.__redirect_std_out_err:
            self.__stdout_reader.join()
            self.__stdout_write_stream.close()
            self.__stderr_reader.join()
            self.__stderr_write_stream.close()
            self.__temp_dir.cleanup()
        return super().__exit__(exc_type, value, traceback)

    @property
    def command(self):
        '''Get invoked command
        '''
        return self.__command

    @property
    def standard_output(self):
        '''Get standard output
        '''
        return self.__stdout

    @property
    def standard_error(self):
        '''Get standard error
        '''
        return self.__stderr


def command_sequence_runner(logger_path: str,
                            command_invoke_sequence: Generator[CommandInvoker, Any, None],
                            ignore_error: bool=False):
    '''Run Generator[CommandInvoker].
    
    :param logger_path: logger path
    :param command_invoke_sequence: a Generator[CommandInvoker] instance
    :param ignore_error: whether to ignore error 
    '''
    while True:
        content = []
        try:
            invoker = next(command_invoke_sequence)
            content.append(f'Run command: {invoker.command}\n')
            content.append(f'{invoker.standard_output}\n')
            content.append(f'{invoker.standard_error}\n')
        except StopIteration:
            break
        except SubprocessError as ex:
            content.append(f'Fail to run command \"{invoker.command}\": {ex}\n')
            if not ignore_error:
                break
            else:
                continue
        finally:
            with open(logger_path, 'a+', encoding='utf-8') as logger:
                logger.writelines(content)
