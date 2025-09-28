from rpyforth.interp import VM, process_tokens, repl

def entry_point(argv):
    vm = VM()
    if len(argv) > 1:
        # run tokens from argv[1:]
        process_tokens(vm, argv[1:])
        return 0
    repl(vm)
    return 0

def target(driver, args):
    return entry_point, None

if __name__ == '__main__':
    import sys
    print(entry_point(sys.argv))
