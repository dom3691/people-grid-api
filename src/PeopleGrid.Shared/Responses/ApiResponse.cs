namespace PeopleGrid.Shared.Responses;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyList<string> Errors)
{
    public static ApiResponse<T> Ok(T? data, string message = "Operation successful") => new(true, message, data, Array.Empty<string>());
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new(false, message, default, errors?.ToArray() ?? Array.Empty<string>());
}
