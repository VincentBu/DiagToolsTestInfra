import os

from CoreFunctionality import common
from CoreFunctionality.cli import CommandInvoker

class CLIDebugger:
    def __init__(self, debugger_path: str):
        debugger_name = os.path.basename(debugger_path)
        if common.rid_os_name() == 'linux-musl':
            raise OSError('Debugging is not supported on Alpine Linux')
        
        assert debugger_name.startswith('cdb') or debugger_name.startswith('lldb')

        self.__debugger_path = debugger_path

    def debug_dump(self, dump_path: str, debug_command_list: list[str]):
        args = []
        if common.rid_os_name().startswith('win'):
            args = [
                self.__debugger_path,
                '-cf', script_path,
                '-z', dump_path
            ]
        elif common.rid_os_name().startswith('linux') or \
            common.rid_os_name().startswith('osx'):
            args = [
                self.__debugger_path,
                '-s', script_path,
                '-c', dump_path,
                '--batch'
            ]

        with CommandInvoker(args) as ci:
            ci.communicate()
            return ci
