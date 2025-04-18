﻿# test_fixtures_mark.py
import pytest
from calculator import Calculator


@pytest.mark.demo
class TestCalculator:
    def test_add(self):
        target = Calculator()
        assert target.add(2, 3) == 5

    def test_subtract(self):
        target = Calculator()
        assert target.subtract(5, 3) == 2

    def test_multiply(self):
        target = Calculator()
        assert target.multiply(4, 3) == 12

    def test_divide(self):
        target = Calculator()
        assert target.divide(10, 2) == 5

    # @pytest.mark.skip
    @pytest.mark.skip(reason="搞不定，先跳過")
    def test_divide_by_zero(self):
        target = Calculator()
        with pytest.raises(ValueError):
            target.divide(10, 0)
