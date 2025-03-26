import os
from tempfile import TemporaryDirectory
from threading import Thread
from subprocess import Popen, PIPE

class CommandInvoker(Popen):
    def __init__(self, args: list[str], cwd=None, env=None, redirect_std_out_err=True, silent=True):
        self.__redirect_std_out_err = redirect_std_out_err
        self.__silent: bool = silent

        self.__command = ' '.join(args)

        self.__temp_dir = TemporaryDirectory()
        self.__std_out_file = os.path.join(self.__temp_dir.name, 'out.txt')
        self.__std_err_file = os.path.join(self.__temp_dir.name, 'err.txt')
        self.__stdout_write_stream = open(self.__std_out_file, mode='w+') if self.__redirect_std_out_err else None
        self.__stderr_write_stream = open(self.__std_err_file, mode='w+') if self.__redirect_std_out_err else None
        
        self.__stdout: str = ''
        self.__stderr: str = ''
        
        try:
            super().__init__(args,
                            bufsize=1,
                            cwd=cwd,
                            env=env,
                            stdin=PIPE,
                            stdout=self.__stdout_write_stream,
                            stderr=self.__stderr_write_stream,
                            universal_newlines=True)
        except Exception:
            self.__stdout_write_stream.close()
            self.__stdout_write_stream.close()
            self.__temp_dir.cleanup()
            raise
        
        print(f'run command: {self.__command}')
        if redirect_std_out_err:
            self.__stdout_reader = Thread(target=self.__stdout_pipe_consumer)
            self.__stdout_reader.start()
            self.__stderr_reader = Thread(target=self.__stderr_pipe_consumer)
            self.__stderr_reader.start()

    def __stdout_pipe_consumer(self):
        try:
            with open(self.__std_out_file, mode='r+') as stdout_read_stream:
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
            with open(self.__std_err_file, mode='r+') as stderr_read_stream:
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
        return self.__command
    
    @property
    def standard_output(self):
        return self.__stdout
        
    @property
    def standard_error(self):
        return self.__stderr