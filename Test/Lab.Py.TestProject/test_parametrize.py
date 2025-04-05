# test_parametrize.py
import pytest
from calculator import Calculator


@pytest.mark.parametrize("first, second, expected", [
    (2, 3, 5),
    (-1, 1, 0),
    (0, 0, 0),
])
def test_add(first, second, expected):
    calculator = Calculator()
    assert calculator.add(first, second) == expected
