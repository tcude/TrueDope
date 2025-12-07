namespace TrueDope.Api.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class ApiResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null) => new()
    {
        Success = true,
        Message = message
    };

    public static ApiResponse Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public ApiError? Error { get; set; }
    public string? Message { get; set; }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

public class PaginatedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Items { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
