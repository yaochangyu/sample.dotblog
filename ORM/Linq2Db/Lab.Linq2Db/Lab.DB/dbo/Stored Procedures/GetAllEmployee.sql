CREATE PROCEDURE dbo.GetEmployee

AS
	SELECT
  Id
 ,Name
 ,Age
 ,SequenceId
 ,Remark
FROM dbo.Employee;
GO