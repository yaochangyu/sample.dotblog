CREATE TABLE [dbo].[Order] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [Employee_Id] UNIQUEIDENTIFIER NULL,
    [OrderTime]   DATETIME         NULL,
    [Remark]      NVARCHAR (50)    NULL,
    [SequenceId]  BIGINT           IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_dbo.Order] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Order_Employee_id] FOREIGN KEY ([Employee_Id]) REFERENCES [dbo].[Employee] ([Id])
);

