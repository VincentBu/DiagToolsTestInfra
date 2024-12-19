import glob
from typing import Union
from urllib import request

from utility.cli import CommandInvoker
from utility.DotNet import infrastructure


def install_tool(dotNet_root: str, 
                 tool_name: str, 
                 tool_root: str, 
                 tool_version: str, 
                 tool_feed: str) -> CommandInvoker:
    options = [
        'tool',
        'install', tool_name,
        '--tool-path', tool_root,
        '--version', tool_version,
        '--add-source', tool_feed
    ]
    invoker = infrastructure.run_dot_net_command(
        dotNet_root, options,
        wait_for_exit=True,
        silent_run=False
    )
    return invoker


def get_tool_IL_path(tool_name: str, tool_version: str, tool_root: str) -> Union[str, Exception]:
    tool_dll_path_template = (
        f'{tool_root}/.store/{tool_name}'
        f'/{tool_version}/{tool_name}'
        f'/{tool_version}/tools/*/any/{tool_name}.dll'
    )
    tool_dll_path_candidates = glob.glob(tool_dll_path_template)
    
    if len(tool_dll_path_candidates) < 1:
        return Exception(f'no dll file availble for {tool_name}')
    return tool_dll_path_candidates[0]


def download_perfcollect(perfcollect_path: str) -> Union[str, Exception]:
    try:
        response = request.urlopen(
            'https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect'
        )
        with open(perfcollect_path, 'w+') as f:
            f.write(response.read().decode())
        return perfcollect_path
    except Exception as ex:
        return Exception(f'fail to download perfcollect script: {ex}')
         