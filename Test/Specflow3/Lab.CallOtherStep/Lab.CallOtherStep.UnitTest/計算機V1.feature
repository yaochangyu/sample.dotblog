Feature: 計算機V1
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

Scenario: 加法
	Given I have entered 50 into the calculator
	And I have also entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen

Scenario: 呼叫加法
	Given I press add and the result should be success

