Feature: 計算機基本運算
  作為一個使用者
  我想要使用計算機進行基本運算
  以便能夠快速得到計算結果

  Scenario Outline: 基本四則運算
    Given 我有一個計算機
    When 我輸入第一個數字 <num1>
    And 我輸入第二個數字 <num2>
    And 我選擇運算符號 <operator>
    Then 我應該得到結果 <result>

    Examples:
      | num1 | num2 | operator | result |
      | 5    | 3    | +        | 8      |
      | 10   | 4    | -        | 6      |
      | 6    | 7    | *        | 42     |
      | 20   | 5    | /        | 4      |

  Scenario: 除以零的錯誤處理
    Given 我有一個計算機
    When 我輸入第一個數字 10
    And 我輸入第二個數字 0
    And 我選擇運算符號 /
    Then 我應該看到錯誤訊息 "除數不能為零"
