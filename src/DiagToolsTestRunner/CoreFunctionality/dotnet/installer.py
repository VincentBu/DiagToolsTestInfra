'''DotNet SDK and runtime installer
'''

import os
from urllib import request
from http.client import HTTPResponse

from CoreFunctionality import common
from CoreFunctionality.dotnet.environment import DotNetEnvironment

class DotNetInstaller:
    '''Install .NET SDK and runtime.
    '''
    def __init__(self, dotnet_env: DotNetEnvironment):
        self.__dotnet_env = dotnet_env

    @property
    def dotnet_sdk_download_link(self):
        self.generate_dotnet_sdk_download_link(
            self.__dotnet_env.sdk_full_version,
            self.__dotnet_env.target_rid
        )

    @classmethod
    def generate_dotnet_sdk_download_link(cls, sdk_full_version: str, target_rid: str):
        '''Generate compressed .NET SDK download url \
              according to the given sdk version and target rid.
        
        :param sdk_full_version: generate
        :param target_rid: rid
        :return: .NET SDK download url
        '''
        azure_feed_list = [
            'https://builds.dotnet.microsoft.com/dotnet',
            'https://ci.dot.net/public'
        ]

        for feed in azure_feed_list:
            product_version_query_url = f'{feed}/Sdk/{sdk_full_version}/sdk-productVersion.txt'
            try:
                response: HTTPResponse = request.urlopen(product_version_query_url)
                product_version = response.read().decode().replace('\n', '').replace('\r', '')
                file_extension = DotNetEnvironment.get_compressed_file_extension_by_rid(target_rid)
                return '/'.join(
                    [
                        feed,
                        'Sdk',
                        sdk_full_version,
                        f'dotnet-sdk-{product_version}-{target_rid}{file_extension}'
                    ]
                )
            except Exception:
                continue

        raise ValueError(f'Fail to generate download link for {sdk_full_version}')

    def install_dotnet_sdk(self):
        '''Install .NET SDK according to given DotNetEnvironment instance.
        
        '''
        sdk_download_url = self.generate_dotnet_sdk_download_link(
            self.__dotnet_env.sdk_full_version,
            self.__dotnet_env.target_rid
        )

        os.makedirs(self.__dotnet_env.dotnet_root, exist_ok=True)
        sdk_download_path = os.path.join(
            self.__dotnet_env.dotnet_root,
            os.path.basename(sdk_download_url)
        )
        common.http_download(sdk_download_path, sdk_download_url)

        if sdk_download_path.endswith('.tar.gz'):
            common.extract_tar_gz(sdk_download_path, self.__dotnet_env.dotnet_root)
        elif sdk_download_path.endswith('.zip'):
            common.extract_zip(sdk_download_path, self.__dotnet_env.dotnet_root)
        else:
            raise NotImplementedError(f'not supported compressed file {sdk_download_path}')

        os.remove(sdk_download_path)
