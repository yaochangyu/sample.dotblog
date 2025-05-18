import pytest
from pytest_bdd import scenarios, given, when, then, parsers
from calculator import Calculator

scenarios('./features/calculator.feature')

@pytest.fixture
def calculator():
    return Calculator()

@given("我有一個計算機")
def step_我有一個計算機(calculator):
    return calculator

@when(parsers.parse("我輸入第一個數字 {num1:d}"))
def input_first_number(calculator, num1):
    calculator.num1 = num1

@when(parsers.parse("我輸入第二個數字 {num2:d}"))
def input_second_number(calculator, num2):
    calculator.num2 = num2

@when(parsers.parse("我選擇運算符號 {operator}"))
def select_operator(calculator, operator):
    calculator.operator = operator

@then(parsers.parse("我應該得到結果 {result:d}"))
def check_result(calculator, result):
    if calculator.operator == '+':
        assert calculator.add(calculator.num1, calculator.num2) == result
    elif calculator.operator == '-':
        assert calculator.subtract(calculator.num1, calculator.num2) == result
    elif calculator.operator == '*':
        assert calculator.multiply(calculator.num1, calculator.num2) == result
    elif calculator.operator == '/':
        assert calculator.divide(calculator.num1, calculator.num2) == result

@then(parsers.parse('我應該看到錯誤訊息 "{error_message}"'))
def check_error_message(calculator, error_message):
    with pytest.raises(ValueError) as exc_info:
        calculator.divide(calculator.num1, calculator.num2)
    assert str(exc_info.value) == error_message 