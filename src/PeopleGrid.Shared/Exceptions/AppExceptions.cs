namespace PeopleGrid.Shared.Exceptions;

public abstract class AppException(string message) : Exception(message);
public sealed class NotFoundException(string message) : AppException(message);
public sealed class BusinessRuleException(string message) : AppException(message);
public sealed class ForbiddenException(string message) : AppException(message);
public sealed class UnauthorizedAppException(string message) : AppException(message);
public sealed class ValidationAppException(IDictionary<string, string[]> errors) : AppException("Validation failed")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
