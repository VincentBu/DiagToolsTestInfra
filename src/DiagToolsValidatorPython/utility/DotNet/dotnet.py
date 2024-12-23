import os
import zipfile
import tarfile
import tempfile
from urllib import request
from typing import Iterable, Union

from utility.system_info import SysInfo
from utility import cli


def install_dot_net_SDK(sdk_full_version: str, dotNet_root: str):
    # generate download url
    azure_feed_list = [
        "https://dotnetcli.azureedge.net/dotnet",
        "https://dotnetbuilds.azureedge.net/public"
    ]

    for feed in azure_feed_list:
        product_version_query_url = f'{feed}/Sdk/{sdk_full_version}/sdk-productVersion.txt'
        sdk_product_version = ''
        try:
            response = request.urlopen(product_version_query_url)
            sdk_product_version: str = response.read().decode('utf-8')
            sdk_product_version = sdk_product_version.strip('\r\n')
        except Exception:
            continue

        rid = SysInfo.RID
        package_extension = ''
        if 'win' in rid:
            package_extension = '.zip'
        else:
            package_extension = '.tar.gz'
        SDK_download_url = f'{feed}/Sdk/{sdk_full_version}/dotnet-sdk-{sdk_product_version}-{rid}{package_extension}'

        # download compressed .NET SDK
        chunksize = 67108864 # 64 MB
        temp_download_path = tempfile.mktemp()
        try:
            response = request.urlopen(SDK_download_url)
            with open(temp_download_path, 'wb+')as writer:
                while True:
                    chunk = response.read(chunksize)
                    writer.write(chunk)
                    if len(chunk) < chunksize:
                        break
                    
        except Exception as ex:
            return Exception(f'Fail to download .NET SDK: {ex}')
        
        # extrace compressed SDK to DOTNET_ROOT
        try:
            if zipfile.is_zipfile(temp_download_path):
                with zipfile.ZipFile(temp_download_path, 'r') as zf:
                    zf.extractall(dotNet_root)
            else:
                with tarfile.open(temp_download_path) as tf:
                    tf.extractall(dotNet_root)
            return dotNet_root
        except Exception as ex:
            return Exception(f'Fail to extract {temp_download_path}: {ex}')
    return Exception('Fail to generate .NET SDK download link')


def run_dot_net_command(dotNet_root: str,
                        options: Union[Iterable[str], str],
                        redirect_out_err: bool = True,
                        wait_for_exit: bool = True,
                        silent_run: bool = False,
                        **kwargs):
    if 'env' in kwargs.keys():
        kwargs['env']['DOTNET_ROOT'] = dotNet_root
    else:
        env = os.environ.copy()
        env['DOTNET_ROOT'] = dotNet_root
        kwargs['env'] = env

    dotNet_executable = os.path.join(dotNet_root, f'dotnet{SysInfo.ExecutableExtension}')
    args = [dotNet_executable] + options
    invoker = cli.run_command(args, redirect_out_err=redirect_out_err, wait_for_exit=wait_for_exit, silent_run=silent_run, **kwargs)
    return invoker