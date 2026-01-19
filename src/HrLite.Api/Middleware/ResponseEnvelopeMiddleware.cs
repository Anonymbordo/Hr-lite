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

        await _next(context);

        context.Response.Body = originalBodyStream;
        responseBody.Seek(0, SeekOrigin.Begin);

        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        // Don't wrap if already wrapped or if it's a non-successful response handled by exception middleware
        if (context.Response.StatusCode == (int)HttpStatusCode.OK && 
            !string.IsNullOrEmpty(responseText) &&
            !responseText.TrimStart().StartsWith("{\"success\""))
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            
            object? data = null;
            try
            {
                data = JsonSerializer.Deserialize<object>(responseText);
            }
            catch
            {
                data = responseText;
            }

            var envelope = ApiResponse<object>.SuccessResponse(data, correlationId);
            var envelopedResponse = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(envelopedResponse);
        }
        else
        {
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
