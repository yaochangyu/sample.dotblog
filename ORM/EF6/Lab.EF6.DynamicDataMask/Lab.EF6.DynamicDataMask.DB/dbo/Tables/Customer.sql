CREATE TABLE [dbo].[Customer] (
    [ID]         VARCHAR (11)                                         NOT NULL,
    [Name]       NVARCHAR (10)                                        NULL,
    [Birthday]   DATE MASKED WITH (FUNCTION = 'default()')            NULL,
    [Marriage]   CHAR (1) MASKED WITH (FUNCTION = 'default()')        NULL,
    [Email]      VARCHAR (50)                                         NULL,
    [Tel]        VARCHAR (20) MASKED WITH (FUNCTION = 'default()')    NULL,
    [Salary]     NUMERIC (13, 2) MASKED WITH (FUNCTION = 'default()') NULL,
    [CreditCard] VARCHAR (19)                                         NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
GRANT SELECT
    ON OBJECT::[dbo].[Customer] TO [MaskId]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[Customer] TO [UnmaskId]
    AS [dbo];

