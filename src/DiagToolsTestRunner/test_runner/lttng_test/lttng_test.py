'''Implement LTTng test
'''

import os

from test_runner.lttng_test.config import LTTngTestConfig

def test_lttng(config: LTTngTestConfig):
    '''Test LTTng.
    
    :param config: a LTTngTestConfig instance
    :return: Generator[CommandInvoker, Any, None]
    '''
    rid = config.DotNetEnvironment.target_rid
    sdk_version = config.DotNetEnvironment.sdk_full_version
    trace_path = os.path.join(
        config.TestResultFolder,
        f'trace-net{sdk_version}-{rid}')
    yield config.PerfCollect.collect_trace_for_secs(
        trace_path, 10, redirect_std_out_err=True
    )
