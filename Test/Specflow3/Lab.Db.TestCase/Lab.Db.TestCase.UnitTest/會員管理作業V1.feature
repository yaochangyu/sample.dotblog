Feature: 會員管理作業V1
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

Scenario: 新增一筆會員資料
	Given 前端傳來以下InsertMemberRequest
		| Name | Age | 
		| yao  | 19  | 
	When 調用MemberRepository.Insert
	Then 預期資料庫的Member資料表有以下資料
		| Id | Name | Age | CreateAt | CreateBy  |
		| 1  | yao  | 19  | 1900-1-1 | TEST_USER |

Scenario: 編輯一筆會員資料
	Given 資料庫的Member資料表已存在以下資料
		| Id | Name | Age | 
		| 1  | yao  | 19  | 
	Given 前端傳來以下UpdateMemberRequest
		| Id | Name | Age | 
		| 1  | yao  | 20  | 
	When 調用MemberRepository.Update
	Then 預期資料庫的Member資料表有以下資料
		| Id | Name | Age | CreateAt | CreateBy  | ModifyAt | ModifyBy  |
		| 1  | yao  | 20  | 1900-1-1 | TEST_USER | 1900-1-1 | TEST_USER |

Scenario: 刪除一筆會員資料
	Given 資料庫的Member資料表已存在以下資料
		| Id | Name | Age | 
		| 1  | yao  | 19  | 
	Given 前端傳來以下DeleteMemberRequest
		| Id | 
		| 1  | 
	When 調用MemberRepository.Delete
	Then 預期資料庫的Member資料表有以下資料
		| Id | Name | Age | CreateAt | CreateBy  | ModifyAt | ModifyBy  |
#
#Scenario: 查詢會員資料
#	Given 資料庫的Member資料表已存在以下資料
#		| Id | Name   | Age |
#		| 1  | yao    | 19  |
#		| 2  | kobe   | 20  |
#		| 3  | jordan | 21  | 
#	Given 前端傳來以下FilterMemberRequest
#		| Name | 
#		| yao  | 
#	When Post 'api/members',查詢資料
#	Then 預期查詢結果FilterMemberResponse如下
#		| Id | Name | Age |
#		| 1  | yao  | 19  |
#

