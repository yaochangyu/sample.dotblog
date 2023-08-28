namespace Lab.ModelValidation.API;

public enum FailureCode
{
    Unknown = 0,
    InputValid = 1,
    MemberNotFound,
    MemberAlreadyExist,
    ServerError,
    DataConflict,
    DataConcurrency,
    DataNotFound,
    DbError,
    S3Error
}