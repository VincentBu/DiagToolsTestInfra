'''Define class that represent toml config  
'''

class BaseDotNetSetting:
    '''`DotNet` section
    '''
    VersionList: list[str]

class BaseDiagToolSetting:
    '''`DiagTool` section
    '''
    Version: str
    Feed: str

class BaseAppSetting:
    '''`App` section
    '''
    BuildConfig: str

class BaseTestSetting:
    '''`Test` section
    '''
    TestBed: str
    TestName: str
