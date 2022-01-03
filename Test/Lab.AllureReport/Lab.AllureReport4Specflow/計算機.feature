Feature: 計算機
Simple calculator for adding two numbers

    @mytag
    Scenario: 相加兩個數字
        Given 第一個數字為 50
        And 第二個數字為 70
        When 兩個數字相加
        Then 結果應該為 120

    Scenario Outline: 相加兩個數字(Examples)
        Given 第一個數字為 <First>
        And 第二個數字為 <Second>
        When 兩個數字相加
        Then 結果應該為 <Result>

        Examples:
          | First | Second | Result |
          | 50    | 70     | 120    |
          | 30    | 40     | 70     |
          | 60    | 30     | 90     |