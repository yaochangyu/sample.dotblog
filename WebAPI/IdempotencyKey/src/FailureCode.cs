namespace IdempotencyKey.WebApi;

public enum FailureCode
{
    NotFound,
    DuplicateEmail,
    DbConcurrency,
    DbError,
    ValidationError,
    Unknown
}
