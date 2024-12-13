from easycli import *

from command.diagnostic_tools.diagnostic_tools_test import DiagToolTestCommandRunner

if __name__ == '__main__':
    app = CommandApp()

    app.add_command('test-diag-tools', DiagToolTestCommandRunner)
    
    app.run()
