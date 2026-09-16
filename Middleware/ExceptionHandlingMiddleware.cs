using System.Net;
using System.Text.Json;

namespace ProductionMeeting.Middleware;

// Centralized exception handling: users get a friendly message, developers get a full log entry.
// Never leaks stack traces or exception details to the client.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var wantsJson = context.Request.Headers["Accept"].ToString().Contains("application/json")
                || context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (wantsJson)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again or contact support."
                }));
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
