#!/usr/bin/python3

class MinMenu:
    def __init__(self, title, items, prompt):
        self.title = title
        self.items = items
        self.prompt = prompt

    def get_selection(self):
        if len(self.items) > 1:
            index = self.prompt_for_choice()
        else:
            index = 0

        return index

    def prompt_for_choice(self):
        choice = -1
        while choice < 1 or choice > len(self.items):
            print(self.title)

            i = 0
            for item in self.items:
                i += 1
                print('{:>2}: {}'.format(i, item))

            try:
                choice = int(input(self.prompt))
            except ValueError:
                pass

        return choice - 1

def is_empty(string):
    return not string or string.isspace()

def prompt_for_value(read_function, prompt):
    value = None
    while is_empty(value):
        value = read_function(prompt)

    return value
