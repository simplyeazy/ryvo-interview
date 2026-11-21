using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Api;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedDomainException => (StatusCodes.Status403Forbidden, "Forbidden"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            TimesheetLockedException => (StatusCodes.Status409Conflict, "Timesheet locked"),
            OverlappingTimeEntryException => (StatusCodes.Status409Conflict, "Overlapping entry"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent update"),
            DailyCapExceededException => (StatusCodes.Status422UnprocessableEntity, "Daily cap exceeded"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
