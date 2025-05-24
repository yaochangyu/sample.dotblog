import pytest
from pytest_bdd import scenarios, given, when, then, parsers
from calculator import Calculator

scenarios('./features/calculator.feature')


@pytest.fixture
def calculator():
    return Calculator()


@given('我有一個計算機', target_fixture='context')
def 我有一個計算機(calculator):
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
def 我按下鍵(context):
    num1 = context['num1']
    num2 = context['num2']
    operator = context['operator']
    calculator = context['calculator']

    # 定義運算符對應的計算機方法
    operatorMap = {
        '+': calculator.add,
        '-': calculator.subtract,
        '*': calculator.multiply,
        '/': calculator.divide
    }

    if operator not in operatorMap:
        raise ValueError(f"不支援的運算符號: {operator}")

    try:
        # 直接從字典取得對應的方法並執行
        result = operatorMap[operator](num1, num2)
        context['result'] = result
    except ValueError as e:
        context['error'] = str(e)

@then(parsers.parse("我應該得到結果 {result:d}"))
def 我應該得到結果(context, result):
    assert context['result'] == result


@then(parsers.parse('我應該看到錯誤訊息 "{error_message}"'))
def 我應該看到錯誤訊息(context, error_message):
    assert context['error'] == error_message
