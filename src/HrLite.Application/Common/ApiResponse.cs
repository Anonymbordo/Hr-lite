namespace HrLite.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ErrorDetails? Error { get; set; }
    public string? CorrelationId { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId
        };
    }

    public static ApiResponse<T> ErrorResponse(string code, string message, string? correlationId = null, List<string>? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ErrorDetails
            {
                Code = code,
                Message = message,
                Details = details ?? new List<string>()
            },
            CorrelationId = correlationId
        };
    }
}

public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}
