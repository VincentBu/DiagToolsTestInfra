import zipfile
import tarfile
import tempfile
from urllib import request

from functionality.system_info import SysInfo


def install_dot_net_SDK_download_url(sdk_full_version: str, dotNet_root: str):
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
            ex.add_note('Fail to download .NET SDK')
            return ex
        
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
            ex.add_note(f'Fail to extract file: {temp_download_path}')
            return ex
    return Exception('Fail to generate .NET SDK download link')

