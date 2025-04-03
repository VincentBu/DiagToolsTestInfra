'''Parse config file and generate information for test runner
'''

import os

from test_runner import config_type_mapping
from core_functionality import common
from core_functionality.tracing import PerfCollect
from core_functionality.dotnet.environment import DotNetEnvironment
from core_functionality.dotnet.installer import DotNetInstaller
from core_functionality.dotnet.app import DotNetApp


class BaseLTTngTestConfig:
    '''Map to lttng config file.
    '''
    DotNet: config_type_mapping.BaseDotNetSetting
    App: config_type_mapping.BaseAppSetting
    Test: config_type_mapping.BaseTestSetting


class LTTngTestConfig:
    '''Test config for LTTng test runner.
    '''
    DotNetEnvironment: DotNetEnvironment
    Installer: DotNetInstaller
    GCPerfsim: DotNetApp
    PerfCollect: PerfCollect
    TestResultFolder: str


def generate_lttng_test_config(config_file_path: str) -> list[LTTngTestConfig]:
    '''Parse config file and generate test config for LTTng test runner.
    
    :param toml_file_path: path of toml file
    :return: a list of LTTngTestConfig instance
    '''
    base_config: BaseLTTngTestConfig = common.parse_toml(config_file_path)

    config_list = list()
    for version in base_config.DotNet.VersionList:
        dotnet_root = os.path.join(
            base_config.Test.TestBed,
            f'.NET-sdk-{version}'
        )
        target_rid = f'{common.rid_os_name()}-{common.rid_machine_name()}'
        build_config = base_config.App.BuildConfig
        env = DotNetEnvironment(dotnet_root, version, target_rid)
        installer = DotNetInstaller(base_config.Test.TestBed, env)
        app_root = os.path.join(base_config.Test.TestBed, f'gcperfsim-{version}')
        gcperfsim_app = DotNetApp(env, app_root, 'console', build_config, name='gcperfsim')
        test_result_folder = os.path.join(base_config.Test.TestBed, f'TestResult-{version}')
        perfcollect_path = os.path.join(base_config.Test.TestBed, 'perfcollect')

        config = LTTngTestConfig()
        config.DotNetEnvironment = env
        config.Installer = installer
        config.GCPerfsim = gcperfsim_app
        config.PerfCollect = PerfCollect(perfcollect_path)
        config.TestResultFolder = test_result_folder
        config_list.append(config)
    return config_list
