CREATE TABLE [dbo].[Memebr] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [SequenceId] BIGINT           IDENTITY (1, 1) NOT NULL,
    [Remark]     NVARCHAR (100)   NULL,
    PRIMARY KEY NONCLUSTERED ([Id] ASC)
);


GO
CREATE CLUSTERED INDEX [CLIX_Memebr_SequenceId]
    ON [dbo].[Memebr]([SequenceId] ASC);

