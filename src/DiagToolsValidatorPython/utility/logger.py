import logging
from typing import Any, Generator

from utility.cli import CommandInvoker


class ProgressRecorder():
    def __init__(self, logger_path: str, series_connection: bool = True):
        self.__run_next = True
        self.__is_series_connection = series_connection
        self.__logger_path = logger_path

    @property
    def is_series_connection(self):
        return self.__is_series_connection 

    def advance(self, progresses: Generator[Any, Any, None]):
        last_progress = None

        if self.__is_series_connection == True and self.__run_next == False:
            return last_progress
        
        for progress in progresses:
            if isinstance(progress, CommandInvoker):
                with open(self.__logger_path, '+a') as fp:
                    fp.write(f'\n\nRun command `{progress.command}`\n')
                    stdout_in_lines = progress.standard_output.split('\n')
                    for line in stdout_in_lines:
                        fp.write(f'    {line}\n')
                    
                    if progress.standard_error != '':
                        stderr_in_lines = progress.standard_error.split('\n')
                        for line in stderr_in_lines:
                            fp.write(f'    {line}\n')

                        if self.__is_series_connection == True:
                            self.__run_next = False

                    if progress.exception is not None:
                        fp.write(f'{progress.exception}\n')

                        if self.__is_series_connection == True:
                            self.__run_next = False

            elif isinstance(progress, Exception):
                with open(self.__logger_path, '+a') as fp:
                    fp.write(f'{progress}\n')
                if self.__is_series_connection == True:
                    self.__run_next = False
            elif isinstance(progress, str):
                with open(self.__logger_path, '+a') as fp:
                    fp.write(f'{progress}\n')
            else:
                continue
                
            last_progress = progress
            
            if self.__is_series_connection == True and self.__run_next == False:
                break

        return last_progress
            
class AppLogger(logging.Logger):
    def __init__(self, name: str, logger_file_path: str, level: int = logging.INFO) -> None:
        logging.root.setLevel(logging.NOTSET)
        super().__init__(name, level)

        FILE_LOG_FORMAT = '%(levelname)s> %(module)s.%(funcName)s:\n%(message)s'
        file_log_handler = logging.FileHandler(filename=logger_file_path)
        file_log_handler.setFormatter(logging.Formatter(FILE_LOG_FORMAT))
        file_log_handler.setLevel(logging.DEBUG)
        self.addHandler(file_log_handler)