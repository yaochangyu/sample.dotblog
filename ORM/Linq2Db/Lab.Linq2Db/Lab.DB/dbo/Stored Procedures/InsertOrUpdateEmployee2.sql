CREATE PROCEDURE dbo.InsertOrUpdateEmployee2
@EmployeeType InsertOrUpdateEmployeeType READONLY
AS
BEGIN
    INSERT INTO employee
    SELECT * FROM @EmployeeType
END
