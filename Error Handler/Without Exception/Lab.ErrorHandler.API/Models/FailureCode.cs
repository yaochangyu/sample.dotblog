namespace Lab.ErrorHandler.API.Models;

public enum FailureCode
{
    UnknownError = 0,
    InputInvalid = 1,
    MemberNotFound,
    MemberAlreadyExist,
    ServerError,
    DataConflict,
    DataConcurrency,
    DataNotFound,
    DbError,
    S3Error,
    CellphoneFormatInvalid
}