using HrLite.Application.Common;
using HrLite.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace HrLite.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        
        HttpStatusCode statusCode;
        string errorCode;
        string message;
        List<string> details = new();

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                errorCode = "VALIDATION_ERROR";
                message = validationEx.Message;
                details = validationEx.Errors;
                _logger.LogWarning(validationEx, "Validation error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case NotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                errorCode = "NOT_FOUND";
                message = notFoundEx.Message;
                _logger.LogWarning(notFoundEx, "Resource not found. CorrelationId: {CorrelationId}", correlationId);
                break;

            case BusinessException businessEx:
                statusCode = HttpStatusCode.Conflict;
                errorCode = "BUSINESS_RULE_VIOLATION";
                message = businessEx.Message;
                details = businessEx.Details;
                if (!string.IsNullOrWhiteSpace(businessEx.ErrorCode) && businessEx.ErrorCode != "BUSINESS_RULE_VIOLATION")
                {
                    details.Add($"Code: {businessEx.ErrorCode}");
                }
                _logger.LogWarning(businessEx, "Business rule violation. CorrelationId: {CorrelationId}", correlationId);
                break;

            case UnauthorizedException unauthorizedEx:
                statusCode = HttpStatusCode.Unauthorized;
                errorCode = "UNAUTHORIZED";
                message = unauthorizedEx.Message;
                _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
                break;

            case ForbiddenException forbiddenEx:
                statusCode = HttpStatusCode.Forbidden;
                errorCode = "FORBIDDEN";
                message = forbiddenEx.Message;
                _logger.LogWarning(forbiddenEx, "Forbidden access attempt. CorrelationId: {CorrelationId}", correlationId);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Forbidden;
                errorCode = "FORBIDDEN";
                message = "You do not have permission to access this resource.";
                _logger.LogWarning(exception, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                errorCode = "INTERNAL_ERROR";
                message = "An unexpected error occurred. Please try again later.";
                _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.ErrorResponse(errorCode, message, correlationId, details);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
