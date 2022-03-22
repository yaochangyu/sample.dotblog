namespace Lab.ChangeTracking.Domain;

public record Error<T>(T Code, object Message);