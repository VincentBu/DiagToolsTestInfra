import traceback

ex = Exception('exp')

print(f'throw ex: {ex}\n{traceback.print_exc()}')

ex.add_note('add additional info')

print(f'throw ex: {ex}\n{traceback.print_exc()}')