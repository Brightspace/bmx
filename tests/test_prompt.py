import contextlib
import io
import unittest
from unittest.mock import (Mock, patch)

from .context import bmx
import bmx.prompt as prompt

TITLE = 'title'
PROMPT = 'prompt'
VALUE = 'value'


class PromptTests(unittest.TestCase):
    def test_prompt_for_value_should_return_value_always(self):
        def read_function(prompt):
            self.assertEqual(PROMPT, prompt)
            return VALUE

        self.assertEqual(
            VALUE,
            prompt.prompt_for_value(read_function, PROMPT))

    def test_invalid_minmenu(self):
        with self.assertRaises(ValueError):
            prompt.MinMenu(TITLE, [], PROMPT)

    @patch('bmx.prompt.MinMenu.prompt_for_choice')
    def test_get_selection_should_return_0_when_items_is_empty_or_single(self, mock_prompt_for_choice):
            menu = prompt.MinMenu(TITLE, ['foo'], PROMPT)

            self.assertEqual(0, menu.get_selection())
            mock_prompt_for_choice.assert_not_called()

    @patch('bmx.prompt.MinMenu.prompt_for_choice')
    def test_get_selection_should_prompt_when_items_has_multiple(self, mock_prompt_for_choice):
        items = ['foo', 'bar']
        expected_selection = 1

        menu = bmx.prompt.MinMenu(TITLE, items, PROMPT)
        mock_prompt_for_choice.return_value = expected_selection

        self.assertEqual(expected_selection, menu.get_selection())

    @patch('bmx.prompt.MinMenu.prompt_for_choice')
    def test_get_selection_should_prompt_when_forced(self, mock_prompt_for_choice):
        items = ['foo']
        expected_selection = 1

        menu = bmx.prompt.MinMenu(TITLE, items, PROMPT)
        mock_prompt_for_choice.return_value = expected_selection

        self.assertEqual(expected_selection, menu.get_selection(force_prompt=True))

    def test_prompt_for_choice_should_prompt_for_index_always(self):
        return_value = 2
        read_function = Mock(side_effect = ['retry_when_no_value', return_value])
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
    unittest.main()
