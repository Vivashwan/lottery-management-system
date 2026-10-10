namespace ex1.Exceptions;

/// <summary>A requested resource does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The request conflicts with the current state, e.g. a prize was already drawn. Mapped to HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>The request breaks a business rule, e.g. drawing a prize nobody bought tickets for. Mapped to HTTP 400.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
