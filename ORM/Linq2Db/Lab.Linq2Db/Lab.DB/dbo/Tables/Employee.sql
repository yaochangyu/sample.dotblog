CREATE TABLE [dbo].[Employee] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [Name]       NVARCHAR (50)    NULL,
    [Age]        INT              NULL,
    [SequenceId] BIGINT           IDENTITY (1, 1) NOT NULL,
    [Remark]     NVARCHAR (50)    NULL,
    CONSTRAINT [PK_dbo.Employee] PRIMARY KEY NONCLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE CLUSTERED INDEX [CLIX_Employee_SequenceId]
    ON [dbo].[Employee]([SequenceId] ASC);

