import os
import time

import meta
from utility import cli
from utility.DotNet import dotnet_tool, infrastructure
from core_functionality.configuration.diagnostic_tools.diagnostic_tools_test_configuration import DiagToolTestCommandConfiguration


def install_dot_Net_SDK(configuration: DiagToolTestCommandConfiguration):
    sdk_full_version = configuration.dotnet_sdk_version
    dotNet_root = configuration.dotnet_root
    msg = f'install .NET SDK {sdk_full_version} to {dotNet_root}'
    print(f'\n\n{msg}')

    sdk_root = infrastructure.install_dot_net_SDK(sdk_full_version, dotNet_root)
    if isinstance(sdk_root, Exception):
        yield sdk_root
    else:
        yield f'{__file__}: {msg}'


def install_diagnostic_tools(configuration: DiagToolTestCommandConfiguration):
    for tool_name in configuration.diag_tool_to_test:
        print(f'\n\ninstall tool {tool_name}')
        yield dotnet_tool.install_tool(configuration.dotnet_root,
                                       tool_name,
                                       configuration.diag_tool_root,
                                       configuration.diag_tool_version,
                                       configuration.diag_tool_feed)


def prepare_target_apps(configuration: DiagToolTestCommandConfiguration):
    # create and build console app
    print(f'\n\ncreate console app')
    yield configuration.console_app.create_new_app()

    base_src_file_path = os.path.join(meta.script_root,
                                      'command',
                                      'diagnostic_tools',
                                      'TargetApps',
                                      'consoleapp',
                                      'Program.cs.txt')
    target_src_file_path = os.path.join(configuration.console_app.app_root, 'Program.cs')
    with open(base_src_file_path, 'r') as reader, open(target_src_file_path, 'w') as writer:
        writer.write(reader.read())

    print(f'\n\nbuild console app')
    yield configuration.console_app.build_app('Debug')

    # create and build gcdumpplayground
    print(f'\n\ncreate gcdumpplayground')
    yield configuration.gcdumpplayground_app.create_new_app()

    base_src_file_path = os.path.join(meta.script_root,
                                      'command',
                                      'diagnostic_tools',
                                      'TargetApps',
                                      'GCDumpPlayground2',
                                      'Program.cs.txt')
    target_src_file_path = os.path.join(configuration.gcdumpplayground_app.app_root, 'Program.cs')
    with open(base_src_file_path, 'r') as reader, open(target_src_file_path, 'w') as writer:
        writer.write(reader.read())

    print(f'\n\nbuild gcdumpplayground')
    yield configuration.gcdumpplayground_app.build_app('Debug')

    # create and build webapp
    print(f'\n\ncreate webapp')
    yield configuration.webapp_app.create_new_app()

    print(f'\n\nbuild webapp')
    yield configuration.webapp_app.build_app('Debug')


def run_webapp(configuration: DiagToolTestCommandConfiguration):
    print(f'\n\nrun webapp')
    env = os.environ.copy()
    env['DOTNET_ROOT'] = configuration.dotnet_root

    app_executable = configuration.webapp_app.get_app_executable('Debug')
    if isinstance(app_executable, Exception):
        return app_executable

    webapp_invoker = cli.run_command(
        [app_executable],
        redirect_out_err = True,
        wait_for_exit = False,
        silent_run = True,
        env=env)
    
    if webapp_invoker.exception is not None:
        return webapp_invoker.exception

    while True:
        if 'Application started' in webapp_invoker.standard_output:
            break
        else:
            time.sleep(2)

    return webapp_invoker


def run_gcdump_playground(configuration: DiagToolTestCommandConfiguration):
    print(f'\n\nrun GCDumpPlayground2')
    env = os.environ.copy()
    env['DOTNET_ROOT'] = configuration.dotnet_root

    app_executable = configuration.gcdumpplayground_app.get_app_executable('Debug')
    if isinstance(app_executable, Exception):
        return app_executable

    gcdumpplayground_app_invoker = cli.run_command(
        [app_executable],
        redirect_out_err = True,
        wait_for_exit = False,
        silent_run = True,
        env=env)
    
    if gcdumpplayground_app_invoker.exception is not None:
        return gcdumpplayground_app_invoker.exception

    while True:
        if 'Pause for gcdumps' in gcdumpplayground_app_invoker.standard_output:
            break
        else:
            time.sleep(2)

    return gcdumpplayground_app_invoker