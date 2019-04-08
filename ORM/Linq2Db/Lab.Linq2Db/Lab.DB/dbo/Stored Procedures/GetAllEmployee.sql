CREATE PROCEDURE [dbo].[GetAllEmployee]

AS
	SELECT
		 Id
		,Name
		,Age
		,SequenceId
		,Remark
	FROM 
		dbo.Employee;
GO