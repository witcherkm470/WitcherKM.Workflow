using Microsoft.EntityFrameworkCore;
using WitcherKM.Workflow.Application;
using WitcherKM.Workflow.Domain;

namespace WitcherKM.Workflow.Api.Middleware;

public sealed class WorkflowExceptionMiddleware(RequestDelegate next, ILogger<WorkflowExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (WorkflowException exception) { await WriteError(context, exception.StatusCode, exception.Message); }
        catch (DomainRuleException exception) { await WriteError(context, 400, exception.Message); }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Workflow data conflict");
            await WriteError(context, 409, "Изменение конфликтует с текущими данными.");
        }
        catch (BadHttpRequestException exception) { await WriteError(context, 400, exception.Message); }
    }

    private static Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { message });
    }
}
