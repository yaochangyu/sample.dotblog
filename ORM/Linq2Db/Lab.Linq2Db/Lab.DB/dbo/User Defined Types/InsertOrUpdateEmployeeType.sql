CREATE TYPE [dbo].[InsertOrUpdateEmployeeType] AS TABLE (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Name]       NVARCHAR (50)    NULL,
    [Age]        INT              NULL,
    --[SequenceId] BIGINT           IDENTITY (1, 1) NOT NULL,
    [Remark]     NVARCHAR (50)    NULL
   
	);
GO
--CREATE TYPE [dbo].[EmployeeType2] AS TABLE (
--    [Id]         UNIQUEIDENTIFIER NOT NULL,
--    [Name]       NVARCHAR (50)    NULL,
--    [Age]        INT              NULL,
--    [SequenceId] BIGINT           ,
--    [Remark]     NVARCHAR (50)    NULL
   
--	);
