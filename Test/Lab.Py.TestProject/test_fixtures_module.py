# test_fixtures_module.py
import pytest
from calculator import Calculator


@pytest.fixture(scope="module")
def setup_and_cleanup_module():
    print("\n")
    print("當前 .py 檔，class 和 function 只執行一次設置\n")
    yield
    print("\n")
    print("當前 .py 檔，class 和 function 只執行一次清理\n")


def test_add_1(setup_and_cleanup_module):
    target = Calculator()
    assert target.add(2, 3) == 5


class TestCalculator:
    def test_add_2(self, setup_and_cleanup_module):
        target = Calculator()
        assert target.add(2, 3) == 5

    def test_subtract(self, setup_and_cleanup_module):
        target = Calculator()
        assert target.subtract(5, 3) == 2

    def test_multiply(self, setup_and_cleanup_module):
        target = Calculator()
        assert target.multiply(4, 3) == 12

    def test_divide(self, setup_and_cleanup_module):
        target = Calculator()
        assert target.divide(10, 2) == 5

    def test_divide_by_zero(self, setup_and_cleanup_module):
        target = Calculator()
        with pytest.raises(ValueError):
            target.divide(10, 0)
