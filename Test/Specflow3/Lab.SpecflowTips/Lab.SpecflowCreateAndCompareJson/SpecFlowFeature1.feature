Feature: SpecFlowFeature1
Simple calculator for adding two numbers

    Scenario: 建立一筆會員(錯誤)
        Given 已準備 Member 資料(錯誤)
            | Id | Age | IpData                        | Orders         | State  |
            | 1  | 18  | ["192.168.0.1","192.168.0.2"] | [{"Id":"123"}] | Active |
        Then 預期得到 Member 資料(錯誤)
            | Id | Age | IpData                        | Orders         | State  |
            | 1  | 18  | ["192.168.0.1","192.168.0.2"] | [{"Id":"123"}] | Active |

    Scenario: 建立一筆會員(正確)
        Given 已準備 Member 資料(正確)
            | Id | Age | Name                                     | IpData                        | Orders         | State  |
            | 1  | 18  | {"FirstName":"yaochang","LastName":"yu"} | ["192.168.0.1","192.168.0.2"] | [{"Id":"123"}] | Active |
        Then 預期得到 Member 資料(正確)
            | Id | Age | Name                                      | IpData                        | Orders         | State  |
            | 1  | 18  | {"FirstName":"yaochang","LastName":"yu1"} | ["192.168.0.1","192.168.0.3"] | [{"Id":"124"}] | Active |

    Scenario: 建立一筆會員(擴充方法)
        Given 已準備 Member 資料(擴充方法)
            | Id | Age | Name                                     | IpData                        | Orders         | State  |
            | 1  | 18  | {"FirstName":"yaochang","LastName":"yu"} | ["192.168.0.1","192.168.0.2"] | [{"Id":"123"}] | Active |