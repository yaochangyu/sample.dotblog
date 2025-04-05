# test_fixtures.py
import pytest
from calculator import Calculator


@pytest.fixture(scope="function")
def setup_and_cleanup_function():
    print("\n")
    print("每一個 function 個別執行一次設定")
    yield
    print("\n")
    print("每一個 function 個別執行一次清理")


def test_add_1(setup_and_cleanup_function):
    target = Calculator()
    assert target.add(2, 3) == 5


class TestCalculator:
    def test_add(self, setup_and_cleanup_function):
        target = Calculator()
        assert target.add(2, 3) == 5

    def test_subtract(self, setup_and_cleanup_function):
        target = Calculator()
        assert target.subtract(5, 3) == 2

    def test_multiply(self, setup_and_cleanup_function):
        target = Calculator()
        assert target.multiply(4, 3) == 12

    def test_divide(self, setup_and_cleanup_function):
        target = Calculator()
        assert target.divide(10, 2) == 5

    def test_divide_by_zero(self, setup_and_cleanup_function):
        target = Calculator()
        with pytest.raises(ValueError):
            target.divide(10, 0)
