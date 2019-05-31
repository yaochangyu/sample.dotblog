
CREATE PROCEDURE dbo.Insert_Employee
  @Id uniqueidentifier,
  @Name nvarchar(10),
  @Age int,
  @CreateAt datetime,
  @ModifyAt DATETIME NULL,
  @Bonus numeric(3, 1) NULL,
  @Birthday DATE NULL AS
BEGIN
  IF EXISTS
  (SELECT * FROM dbo.Employee WHERE Id = @Id)
    UPDATE dbo.Employee SET
      Name = @Name,
      Age = @Age,
      CreateAt = @CreateAt,
      ModifyAt = @ModifyAt,
      Bonus = @Bonus,
      Birthday = @Birthday

    WHERE Id = @Id
  ELSE
    INSERT INTO dbo.Employee (
      Id,
      Name,
      Age,
      CreateAt,
      ModifyAt,
      Bonus,
      Birthday)
    VALUES(
      @Id,
      @Name,
      @Age,
      @CreateAt,
      @ModifyAt,
      @Bonus,
      @Birthday
      )
END
GO