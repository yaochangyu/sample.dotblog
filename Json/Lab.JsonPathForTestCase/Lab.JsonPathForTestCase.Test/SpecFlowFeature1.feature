Feature: SpecFlowFeature1
Simple calculator for adding two numbers

    Scenario: 用 Table 驗證資料
        When 調用端發送 "Get" 請求至 "api/member"
        Then 預期得到回傳 Member 結果為
            | Age | Birthday                    |
            | 18  | 1/1/2000 12:00:00 AM +00:00 |
        Then 預期得到回傳 Member.FullName 結果為
            | FirstName | LastName |
            | John      | Doe      |

    Scenario: 用 JsonDiff 驗證資料
        When 調用端發送 "Get" 請求至 "api/member"
        Then 預期回傳內容為
        """
        {
            "Id": 1,
            "Age": 18,
            "Birthday": "2000-01-01T00:00:00+00:00",
             "FullName": {
                "FirstName": "John",
                "LastName": "Doe"
            }
        }
        """

    Scenario: 用 JsonPath 驗證資料
        When 調用端發送 "Get" 請求至 "api/member"
        Then 預期回傳內容中路徑 "$.Age" 的數值等於 "18"
        Then 預期回傳內容中路徑 "$.Birthday" 的時間等於 "2000-01-01T00:00:00+00:00"
        Then 預期回傳內容中路徑 "$.FullName.FirstName" 的字串等於 "John"
        Then 預期回傳內容中路徑 "$.FullName.LastName" 的字串等於 "Doe"