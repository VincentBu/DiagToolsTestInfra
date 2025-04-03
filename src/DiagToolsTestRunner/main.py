'''App entry
'''

from eazycli import ConsoleApp
from commands import lttng_test_command

if __name__ == '__main__':
    app = ConsoleApp()
    app.add_command('test-lttng', lttng_test_command.TestLTTngCommandRunner)
    app.run()
