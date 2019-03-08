CREATE TABLE [dbo].[Identity] (
    [Employee_Id] UNIQUEIDENTIFIER NOT NULL,
    [Account]     NVARCHAR (50)    NOT NULL,
    [Password]    NVARCHAR (50)    NOT NULL,
    [SequenceId]  BIGINT           IDENTITY (1, 1) NOT NULL,
    [Remark]      NVARCHAR (50)    NULL,
    CONSTRAINT [PK_dbo.Identity] PRIMARY KEY NONCLUSTERED ([Employee_Id] ASC),
    CONSTRAINT [FK_Identity_Employee_Id] FOREIGN KEY ([Employee_Id]) REFERENCES [dbo].[Employee] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Employee_Id]
    ON [dbo].[Identity]([Employee_Id] ASC);

