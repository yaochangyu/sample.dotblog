USE [Lab.EF6.DynamicDataMask]
CREATE USER UnmaskId FROM LOGIN UnmaskId
CREATE USER MaskId FROM LOGIN MaskId

ALTER ROLE db_datareader ADD MEMBER UnmaskId
ALTER ROLE db_datawriter ADD MEMBER UnmaskId
--
ALTER ROLE db_datareader ADD MEMBER MaskId
ALTER ROLE db_datawriter ADD MEMBER MaskId

GRANT SELECT ON Customer TO UnmaskId
GRANT SELECT ON Customer TO MaskId

--取消遮罩
GRANT UNMASK TO UnmaskId

--恢復遮罩
REVOKE UNMASK TO MaskId
GO