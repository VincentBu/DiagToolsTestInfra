import time
from utility import cli
from utility.DotNet import infrastructure, dotnet_tool
from core_functionality.job.diagnostic_tools import initialize
from core_functionality.configuration.diagnostic_tools.diagnostic_tools_test_configuration import DiagToolTestCommandConfiguration


def test_dotnet_counters(configuration: DiagToolTestCommandConfiguration):
    print(f'\n\n{__file__}: test dotnet-counters')
    tool_IL_path = dotnet_tool.get_tool_IL_path(
        'dotnet-counters',
        configuration.diag_tool_version, 
        configuration.diag_tool_root
    )
    yield tool_IL_path

    webapp_invoker = initialize.run_webapp(configuration)
    yield webapp_invoker

    sync_options_list = [
        [tool_IL_path, '--help'],
        [tool_IL_path, 'list'],
        [tool_IL_path, 'ps'],
    ]
    for options in sync_options_list:
        invoker = infrastructure.run_dot_net_command(
            configuration.dotnet_root,
            options,
            wait_for_exit=True,
            silent_run=False,
            cwd=configuration.test_result_folder
        )
        yield invoker

    async_args_list = [
        [tool_IL_path, 'collect', '-p', str(webapp_invoker.process.pid), '-o', 'webapp_counter.csv'],
        [tool_IL_path, 'monitor', '-p', str(webapp_invoker.process.pid)],
    ]
    for options in async_args_list:
        invoker = infrastructure.run_dot_net_command(
            configuration.dotnet_root,
            options,
            redirect_out_err=False,
            wait_for_exit=False,
            silent_run=False,
            cwd=configuration.test_result_folder
        )
        time.sleep(10)
        cli.close_command_invoker(invoker)
        time.sleep(3)
        
    cli.close_command_invoker(webapp_invoker)

    console_app_executable = configuration.console_app.get_app_executable('Debug')
    yield console_app_executable

    async_args_list = [
        [tool_IL_path, 'collect', '--', console_app_executable, '-o', 'console_counter.csv'],
        [tool_IL_path, 'monitor', '--', console_app_executable],
    ]
    for options in async_args_list:
        invoker = infrastructure.run_dot_net_command(
            configuration.dotnet_root,
            options,
            redirect_out_err=False,
            wait_for_exit=True,
            silent_run=False,
            cwd=configuration.test_result_folder
        )
        time.sleep(3)