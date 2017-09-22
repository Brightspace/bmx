#!/usr/bin/python3

class MinMenu:
    def __init__(self, title, items, prompt, read_function=input):
        if not items or len(items) < 1:
            raise ValueError("At least one item required.")
        self.title = title
        self.items = items
        self.prompt = prompt
        self.read_function = read_function

    def get_selection(self, force_prompt=False):
        if len(self.items) > 1 or force_prompt:
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
                choice = int(self.read_function(self.prompt))
            except ValueError:
                pass

        return choice - 1

def prompt_for_value(read_function, prompt):
    while True:
        value = read_function(prompt)
        if value.strip():
            break
    return value
