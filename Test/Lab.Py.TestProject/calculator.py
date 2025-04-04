# calculator.py
class Calculator:
    """
    一個簡單的計算器類，提供基本的數學運算功能。
    提供的運算包括加法、減法、乘法和除法。
    使用範例：
    calc = Calculator()
    result = calc.add(5, 3)
    result = calc.subtract(5, 3)
    result = calc.multiply(5, 3)
    result = calc.divide(5, 3)

    """

    def add(self,a, b):
        return a + b

    def subtract(self,a, b):
        return a - b

    def multiply(self,a, b):
        return a * b

    def divide(self,a, b):
        if b == 0:
            raise ValueError("除數不能為零")
        return a / b
