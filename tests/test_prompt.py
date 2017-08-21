import contextlib
import io
import unittest
from unittest.mock import Mock

import bmx.prompt

TITLE = 'title'
PROMPT = 'prompt'
VALUE = 'value'


class PromptTests(unittest.TestCase):
    def test_is_empty_should_return_true_when_string_is_empty(self):
        for i in [None, '', "\t  \t  ", "  \n  "]:
            with self.subTest(i=i):
                self.assertTrue(bmx.prompt.is_empty(i))

    def test_is_empty_should_return_false_when_string_is_not_empty(self):
        self.assertFalse(bmx.prompt.is_empty('foo'))

    def test_prompt_for_value_should_return_value_always(self):
        def read_function(prompt):
            self.assertEqual(PROMPT, prompt)

            return VALUE

        self.assertEqual(
            VALUE,
            bmx.prompt.prompt_for_value(read_function, PROMPT)
        )

    def test_get_selection_should_return_0_when_items_is_empty(self):
        for i in [[], ['foo']]:
            with self.subTest(i=i):
                menu = bmx.prompt.MinMenu(TITLE, i, PROMPT)
                menu.prompt_for_choice = Mock()

                self.assertEqual(0, menu.get_selection())

                menu.prompt_for_choice.assert_not_called()

    def test_get_selection_should_prompt_when_items_has_items(self):
        items = ['foo', 'bar']
        menu = bmx.prompt.MinMenu(TITLE, items, PROMPT)
        menu.prompt_for_choice = Mock(return_value=1)

        self.assertEqual(1, menu.get_selection())

    def test_prompt_for_choice_should_prompt_for_index_always(self):
        return_value = 2
        read_function = Mock(return_value = return_value);
        menu = bmx.prompt.MinMenu(
            TITLE,
            ['one', 'two', 'three'],
            PROMPT,
            read_function
        )

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(return_value - 1, menu.prompt_for_choice())

        read_function.assert_called_with(PROMPT)

if __name__ == '__main__':
    unittest.main();
