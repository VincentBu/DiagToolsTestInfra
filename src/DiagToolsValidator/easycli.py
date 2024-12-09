import sys
import argparse
from abc import abstractmethod
from typing import Callable


class CommandSetting():
    def __init__(self):
        pass
    
    @staticmethod
    def command_option(short_name: str, full_name: str, *co_args, **co_kwargs):
        def decorator(func: Callable):
            def wrapper(*func_args, **func_kwargs):
                co_kwargs['dest'] = func.__name__
                
                try:
                    CommandApp.parser.add_argument(short_name, full_name, *co_args, **co_kwargs)
                except Exception:
                    # ignore conflict arguments
                    ...

                return getattr(CommandApp.args, func.__name__, None)
            return wrapper
        return decorator
    
    def map_args_to_setting(self):
        property_names = [p for p in dir(self.__class__) if isinstance(getattr(self.__class__, p), property)]
        # add argument by calling command_option decorator
        for key in property_names:
            getattr(self, key)

        CommandApp.args = CommandApp.parser.parse_args(sys.argv[2:])


class CommandRunner(CommandSetting):
    def __init__(self):
        super().__init__()
        
    @abstractmethod
    def execute(self, *args, **kwargs):
        ...


class CommandApp():
    parser = argparse.ArgumentParser()
    args = None
    def __init__(self):
        self.__command_name_runner_dict = dict()

    def add_command(self,
                    command_name: str,
                    command_runner_type: CommandRunner.__class__):
        self.__command_name_runner_dict[command_name] = command_runner_type

    def run(self):
        command_name = sys.argv[1]
        command_runner_type: type[CommandRunner] = self.__command_name_runner_dict[command_name]
        command_setting_type = command_runner_type.__base__
        command_setting = command_setting_type()
        command_setting.map_args_to_setting()
        command_runner = command_runner_type()
        command_runner.execute(command_setting)
