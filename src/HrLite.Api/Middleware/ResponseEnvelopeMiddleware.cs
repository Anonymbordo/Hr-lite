using HrLite.Application.Common;
using System.Net;
using System.Text.Json;

namespace HrLite.Api.Middleware;

public class ResponseEnvelopeMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseEnvelopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            // Always restore the original stream so upstream middleware (e.g., exception handling)
            // can write directly to the real response body.
            context.Response.Body = originalBodyStream;
        }

        responseBody.Seek(0, SeekOrigin.Begin);

        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        var isSuccessStatus = context.Response.StatusCode >= (int)HttpStatusCode.OK
            && context.Response.StatusCode < (int)HttpStatusCode.MultipleChoices;
        var isAlreadyWrapped = responseText.TrimStart().StartsWith("{\"success\"");

        // Don't wrap if already wrapped or if it's a non-successful response handled by exception middleware
        if (isSuccessStatus && !isAlreadyWrapped)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            
            object? data = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try
                {
                    data = JsonSerializer.Deserialize<object>(responseText);
                }
                catch
                {
                    data = responseText;
                }
            }

            var envelope = ApiResponse<object>.SuccessResponse(data ?? new { }, correlationId);
            var envelopedResponse = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            if (context.Response.StatusCode == (int)HttpStatusCode.NoContent)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            await context.Response.WriteAsync(envelopedResponse);
        }
        else
        {
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
