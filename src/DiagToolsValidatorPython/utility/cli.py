import tempfile
from subprocess import Popen, PIPE
from threading import Thread
from typing import Iterable, Union


class CommandInvoker():
    def __init__(self, args: Iterable[str], redirect_out_err: bool = True, silent_run: bool = False, **kwargs):
        self.__silent_run = silent_run
        self.__command = ' '.join(args)
        self.__standard_output = ''
        self.__standard_error = ''
        self.__process = None
        self.__exception = None
        self.__standard_output_writer = None
        self.__standard_output_reader = None
        self.__standard_error_writer = None
        self.__standard_error_reader = None 

        self.__redirect_out_err = redirect_out_err

        try:
            self.__temp_output_path = tempfile.mktemp()
            self.__temp_error_path = tempfile.mktemp()
            self.__standard_output_writer = open(self.__temp_output_path, 'w+')
            self.__standard_error_writer = open(self.__temp_error_path, 'w+')

            kwargs['shell'] = False
            kwargs['stdin'] = PIPE
            print(f'Run command: {self.__command}')
            if redirect_out_err:
                kwargs['stdout'] = self.__standard_output_writer
                kwargs['stderr'] = self.__standard_error_writer
                self.__process = Popen(args, **kwargs)
                self.__standard_output_reader = Thread(target=self.read_standard_output)
                self.__standard_output_reader.start()
                self.__standard_error_reader = Thread(target=self.read_standard_error)
                self.__standard_error_reader.start()
            else:
                kwargs['stdout'] = None
                kwargs['stderr'] = None
                self.__process = Popen(args, **kwargs)

        except Exception as ex:
            self.__exception = Exception(f'Fail to run command `{self.command}`: {ex}')

    def read_standard_output(self):
        with open(self.__temp_output_path, 'r+') as standard_output_reader:
            while standard_output_reader.readable():
                line = standard_output_reader.readline()
                if line != '':
                    self.__standard_output += line
                    if not self.__silent_run:
                        print(f'    {line}', end='')

                with open(self.__temp_output_path, 'r+') as fp:
                    if len(self.__standard_output) == len(fp.read()) and self.__standard_output_writer.closed:
                        break

    def read_standard_error(self):
        with open(self.__temp_error_path, 'r+') as standard_error_reader:
            while standard_error_reader.readable():
                line = standard_error_reader.readline()
                if line != '':
                    self.__standard_error += line
                    if not self.__silent_run:
                        print(f'    {line}', end='')

                with open(self.__temp_error_path, 'r+') as fp:
                    if len(self.__standard_error) == len(fp.read()) and self.__standard_error_writer.closed:
                        break
    
    @property
    def redirect_out_err(self):
        return self.__redirect_out_err

    @property
    def command(self):
        return self.__command
    
    @property
    def standard_output(self):
        return self.__standard_output
    
    @property
    def standard_error(self):
        return self.__standard_error
    
    @property
    def process(self):
        return self.__process
    
    @property
    def exception(self):
        return self.__exception

    @property
    def standard_output_reader(self):
        return self.__standard_output_reader
    
    @property
    def standard_output_writer(self):
        return self.__standard_output_writer
    
    @property
    def standard_error_reader(self):
        return self.__standard_error_reader
    
    @property
    def standard_error_writer(self):
        return self.__standard_error_writer


def release_invoker_resources(command_invoker: CommandInvoker):
    if command_invoker.redirect_out_err and command_invoker.exception is None:
        command_invoker.standard_output_writer.close()
        command_invoker.standard_error_writer.close()
        command_invoker.standard_output_reader.join()
        command_invoker.standard_error_reader.join()


def run_command(args: Union[Iterable[str], str], redirect_out_err: bool = True, wait_for_exit: bool = True, silent_run: bool = False, **kwargs):
    invoker = CommandInvoker(args, redirect_out_err, silent_run, **kwargs)
    if wait_for_exit:
        invoker.process.communicate()
        release_invoker_resources(invoker)

    return invoker


def close_command_invoker(command_invoker: CommandInvoker):
    if command_invoker.exception is None:
        command_invoker.process.terminate()
        command_invoker.process.communicate()
    release_invoker_resources(command_invoker)