'''EazyCLI - a single file module for console app development. 
'''

import sys
import argparse
from abc import ABC, abstractmethod
from typing import Callable, Type


class CommandLineArguments(ABC):
    '''Bind command line arguments with property.
    '''

class _ArgumentOption:
    '''Descriptor which make decorated method a property. 
    '''
    def __init__(self, func: Callable, *args, **kwargs):
        '''
        :param func: decorated method
        :param args: args of ArgumentParser.add_argument
        :param kwargs: kwargs of ArgumentParser.add_argument
        '''
        kwargs['dest'] = func.__name__
        args_parser = argparse.ArgumentParser()
        args_parser.add_argument(*args, **kwargs)
        args, _ = args_parser.parse_known_args()
        self.__value = args.__getattribute__(func.__name__)

    def __get__(self, instance, owner: type):
        if owner.__base__ != CommandLineArguments:
            raise NotImplementedError('Must implement')
        return self.__value


def add_argument(*args, **kwargs):
    '''Decorate method with _ArgumentOption.

    :param args: args of ArgumentParser.add_argument
    :param kwargs: kwargs of ArgumentParser.add_argument
    '''
    def decorator(func: Callable):
        return _ArgumentOption(func, *args, **kwargs)
    return decorator


class CommandRunner:
    '''Run task for specific command.
    '''
    def __init__(self):
        ...

    @abstractmethod
    def execute(self, command_line_arguments: CommandLineArguments):
        '''Implement this method to run task.

        :param command_argument_setting: a CommandLineArguments instance
        '''

class ConsoleApp():
    '''Represent a console app.
    '''
    def __init__(self):
        self.__command_runner_map: dict[str, Type[CommandRunner]] = dict()

    def add_command(self,
                    command_name: str,
                    command_runner_type: Type[CommandRunner]):
        '''Add command to app.

        :param command_name: subcommand name
        :param command_runner_type: type of command runner
        '''
        assert  len(command_runner_type.__bases__) == 2
        assert  any(base_class == CommandRunner for base_class in command_runner_type.__bases__)
        assert  any(base_class.__base__ == \
                    CommandLineArguments for base_class in command_runner_type.__bases__)
        self.__command_runner_map[command_name] = command_runner_type

    def run(self):
        '''Run execute method in CommandRunner.
        '''
        command_name = sys.argv[1]
        command_runner_type = self.__command_runner_map[command_name]

        command_line_argument_type = None
        for base_class in command_runner_type.__bases__:
            if base_class.__base__ == CommandLineArguments:
                command_line_argument_type = base_class
                break
        command_line_argument = command_line_argument_type()

        command_runner = command_runner_type()
        command_runner.execute(command_line_argument)
