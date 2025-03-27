'''Provide common used utilities
'''

import os
import glob
import winreg
import platform
import tarfile
import zipfile
from urllib import request
from http.client import HTTPResponse

__UNAME = platform.uname()
__USERPROFILE = ''
__SHORTOSNAME = ''
match __UNAME.system.lower():
    case 'windows':
        __SHORTOSNAME = 'win'
        __USERPROFILE = os.environ['USERPROFILE']
    case 'darwin':
        __SHORTOSNAME = 'osx'
        __USERPROFILE = os.environ['HOME']
    case 'linux':
        release_files = glob.glob('/etc/*release')
        __USERPROFILE = os.environ['HOME']
        __CONTENT = ''
        for release_file in release_files:
            with open(release_file, 'r', encoding='utf-8') as f:
                __CONTENT += f'{f.read().lower()}\n'
        if 'alpine' in __CONTENT:
            __SHORTOSNAME = 'linux-musl'
        else:
            __SHORTOSNAME = 'linux'
    case _:
        raise ValueError(f'unsupported os: {__UNAME.system}')

__SHORTMACHINENAME = ''
match __UNAME.machine.lower():
    case 'amd64' | 'x86_64':
        __SHORTMACHINENAME = 'x64'
    case 'arm64' | 'aarch64':
        __SHORTMACHINENAME = 'arm64'
    case 'armv7l':
        __SHORTMACHINENAME = 'arm'
    case _:
        raise ValueError(f'unsupported machine: {__UNAME.machine}')


def user_profile():
    '''Get path to user profile

    :return: path to user profile
    '''
    return __USERPROFILE


def rid_os_name():
    '''Get first part of rid that stands for name of operating system
    
    :return: operating systems name
    '''
    return __SHORTOSNAME


def rid_machine_name():
    '''Get second part of rid that stands for CPU architecture
    
    :return: CPU architecture
    '''
    return __SHORTMACHINENAME


def http_download(download_path: str, download_url: str, buffer_size: int=4*1024*1024):
    '''Start download  
    
    :param download_path: download path
    :param download_url: url of resource(http protocal)
    :param buffer_size: buffer size(byte)
    '''
    try:
        response: HTTPResponse = request.urlopen(download_url)
        with open(download_path, 'wb+') as fp:
            while True:
                buffer = response.read(buffer_size)
                fp.write(buffer)
                if len(buffer) < buffer_size:
                    break
    except Exception as ex:
        print(f'fail to download from {download_url}: {ex}')
        raise


def extract_tar_gz(compressed_file_path: str, destination_folder: str):
    '''Extract .tar.gz file
    
    :param compressed_file_path: compressed file path
    :param destination_folder: extract folder
    '''
    with tarfile.open(compressed_file_path, 'r') as tar_ref:
        tar_ref.extractall(destination_folder)


def extract_zip(compressed_file_path: str, destination_folder: str):
    '''Extract .zip file
    
    :param compressed_file_path: compressed file path
    :param destination_folder: extract folder
    '''
    with zipfile.ZipFile(compressed_file_path, 'r') as zip_ref:
        zip_ref.extractall(destination_folder)


class Win32DumpEnvironment():
    '''Active win32 dump generation
    '''
    def __init__(self, dump_folder: str):
        '''
        :param dump_folder: dump folder
        '''
        assert __SHORTOSNAME == 'win'

        self.__dump_folder = dump_folder
        self.__local_dumps_key: winreg.HKEYType = None
        self.__original_dump_folder = ''

    def __enter__(self):
        self.__local_dumps_key = winreg.CreateKeyEx(
            winreg.HKEY_LOCAL_MACHINE,
            r'SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps',
            0,
            winreg.KEY_ALL_ACCESS
        )

        try:
            self.__original_dump_folder, _ = winreg.QueryValueEx(
                self.__local_dumps_key,
                'DumpFolder'
            )
        except LookupError:
            self.__original_dump_folder = ''

        winreg.SetValueEx(
            self.__local_dumps_key,
            'DumpType',
            0,
            winreg.REG_DWORD,
            2
        )
        winreg.SetValueEx(
            self.__local_dumps_key,
            'DumpFolder',
            0,
            winreg.REG_EXPAND_SZ,
            self.__dump_folder
        )
        return self.__local_dumps_key

    def __exit__(self, exc_type, exc_val, exc_tb):
        winreg.SetValueEx(
            self.__local_dumps_key,
            'DumpFolder',
            0,
            winreg.REG_EXPAND_SZ,
            self.__original_dump_folder
        )
        self.__local_dumps_key.Close()
