import glob
import winreg
import platform
import tarfile
import zipfile
from urllib import request
from http.client import HTTPResponse


def http_download(download_path: str, download_url: str, buffer_size: int=4*1024*1024):
    try:
        response: HTTPResponse = request.urlopen(download_url)
        with open(download_path, 'wb+') as fp:
            while True:
                buffer = response.read(buffer_size)
                fp.write(buffer)
                if len(buffer) < buffer_size:
                    break
    except Exception as ex:
        raise Exception(f'fail to download from {download_url}: {ex}')
    

def extract_tar_gz(compressed_file_path: str, destination_folder: str):
    with tarfile.open(compressed_file_path, 'r') as tar_ref:
        tar_ref.extractall(destination_folder)

    
def extract_zip(compressed_file_path: str, destination_folder: str):
    with zipfile.ZipFile(compressed_file_path, 'r') as zip_ref:
        zip_ref.extractall(destination_folder)

    
class PlatformInfo:
    __uname = platform.uname()

    __rid_os_part = '' 
    match __uname.system.lower():
        case 'windows':
            __rid_os_part = 'win'
        case 'darwin':
            __rid_os_part = 'osx'
        case 'linux':
            release_files = glob.glob('/etc/*release')
            content = ''
            for release_file in release_files:
                with open(release_file, 'r') as f:
                    content += f'{f.read().lower()}\n'
            if 'alpine' in content:
                __rid_os_part = 'linux-musl'
            else:
                __rid_os_part = 'linux'
        case _:
            raise Exception(f'unsupported os: {__uname.system}')
    
    __rid_machine_part = ''
    match __uname.machine.lower():
        case 'amd64' | 'x86_64':
            __rid_machine_part = 'x64'
        case 'arm64' | 'aarch64':
            __rid_machine_part = 'arm64'
        case 'armv7l':
            __rid_machine_part = 'arm'
        case _:
            raise Exception(f'unsupported machine: {__uname.machine}')
            
    @property
    def short_os_name(self):
        return self.__rid_os_part
    
    @property
    def short_machine_name(self):
        return self.__rid_machine_part
    

class Win32DumpEnvironment():
    def __init__(self, dump_folder: str):
        assert PlatformInfo.short_os_name == 'win'

        self.__dump_folder = dump_folder
        self.__local_dumps_key: winreg.HKEYType = None

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
        except Exception:
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
    
    def __exit__(self):
        winreg.SetValueEx(
            self.__local_dumps_key,
            'DumpFolder',
            0,
            winreg.REG_EXPAND_SZ,
            self.__original_dump_folder
        )
        self.__local_dumps_key.Close()