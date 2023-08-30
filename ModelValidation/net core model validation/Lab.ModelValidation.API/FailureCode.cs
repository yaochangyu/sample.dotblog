namespace Lab.ModelValidation.API;

public enum FailureCode
{
    Unknown = 0,
    InputInvalid = 1,
    MemberNotFound,
    MemberAlreadyExist,
    ServerError,
    DataConflict,
    DataConcurrency,
    DataNotFound,
    DbError,
    S3Error
}