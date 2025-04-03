'''Tracing tool for .NET app
'''

import os

from core_functionality import common
from core_functionality.cli import CommandInvoker

class PerfCollect:
    '''Tracing with perfcollect.
    '''
    def __init__(self, perfcollect_path: str, install_prerequisites: bool=False):
        '''
        :param perfcollect_path: perfcollect path
        :param install_prerequisites: whether to install prerequisites
        '''
        if not common.rid_os_name().startswith('linux'):
            raise OSError('perfcollect is only supported on Linux')

        self.__perfcollect_download_link = \
            'https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect'
        self.__perfcollect_path = perfcollect_path

        common.http_download(self.__perfcollect_path, self.__perfcollect_download_link)

        enable_execute_args = ['chmod', '+x', self.__perfcollect_path]
        with CommandInvoker(enable_execute_args, os.environ, silent=False) as ci:
            ci.communicate()

        if install_prerequisites:
            install_args = [
                '/bin/bash',
                self.__perfcollect_path,
                'install'
            ]
            with CommandInvoker(install_args, os.environ, silent=False) as ci:
                ci.communicate()

    @property
    def perfcollect_path(self):
        '''Get perfcollect path.
        '''
        return self.__perfcollect_path

    def collect_trace_for_secs(self,
                               trace_path: str,
                               collect_secs: int,
                               redirect_std_out_err: bool=False,
                               silent: bool=False):
        '''Tracing for specified time.

        :param trace_path: path of trace file
        :param collect_secs: time span for tracing(seconds)
        :param redirect_std_out_err: whether to redirect stardard output and err
        :param silent: whether to suppress console output
        '''
        args = [
            '/bin/bash',
            self.__perfcollect_path,
            'collect',
            trace_path,
            '-collectsec',
            str(collect_secs)
        ]
        with CommandInvoker(
            args,
            os.environ,
            redirect_std_out_err=redirect_std_out_err,
            silent=silent
        ) as invoker:
            invoker.communicate()
            return invoker
