import pytest
from pytest_bdd import scenarios, given, when, then, parsers
from calculator import Calculator

scenarios('./features/calculator.feature')


@pytest.fixture
def calculator():
    return Calculator()


@given('我有一個計算機', target_fixture='context')
def step_impl(calculator):
    return {'calculator': calculator}


@when(parsers.parse("我輸入第一個數字 {num1:d}"))
def 我輸入第一個數字(context, num1):
    context['num1'] = num1


@when(parsers.parse("我輸入第二個數字 {num2:d}"))
def 我輸入第二個數字(context, num2):
    context['num2'] = num2


@when(parsers.parse("我選擇運算符號 {operator}"))
def 我選擇運算符號(context, operator):
    context['operator'] = operator


@when("我按下 = 鍵")
def step_impl(context):
    num1 = context['num1']
    num2 = context['num2']
    operator = context['operator']
    calculator = context['calculator']

    if operator == '+':
        result = calculator.add(num1, num2)
    elif operator == '-':
        result = calculator.subtract(num1, num2)
    elif operator == '*':
        result = calculator.multiply(num1, num2)
    elif operator == '/':
        try:
            result = calculator.divide(num1, num2)
        except ValueError as e:
            context['error'] = str(e)
            return
    else:
        raise ValueError(f"Unsupported operator: {operator}")
    context['result'] = result


@then(parsers.parse("我應該得到結果 {result:d}"))
def 我應該得到結果(context, result):
    assert context['result'] == result


@then(parsers.parse('我應該看到錯誤訊息 "{error_message}"'))
def 我應該看到錯誤訊息(context, error_message):
    assert context['error'] == error_message
