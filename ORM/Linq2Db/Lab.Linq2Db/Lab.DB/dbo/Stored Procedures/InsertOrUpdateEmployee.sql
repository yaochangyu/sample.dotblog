CREATE PROCEDURE dbo.InsertOrUpdateEmployee
  @Id uniqueidentifier,
  @Name nvarchar(50),
  @Age int,
  @SequenceId bigint,
  @Remark nvarchar(50) AS
BEGIN
  IF EXISTS
  (SELECT * FROM dbo.Employee WHERE Id = @Id)
    UPDATE dbo.Employee SET
      Name = @Name,
      Age = @Age,
      Remark = @Remark
    WHERE Id = @Id
  ELSE
    INSERT INTO dbo.Employee (
      Id,
      Name,
      Age,
      Remark)
    VALUES(
      @Id,
      @Name,
      @Age,
      @Remark)
END
