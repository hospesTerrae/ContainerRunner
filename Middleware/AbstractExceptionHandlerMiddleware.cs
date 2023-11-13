using System.Net;
using ContainerRunner.Models;

namespace ContainerRunner.Middleware;

public abstract class AbstractExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public AbstractExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public abstract (HttpStatusCode, Reason) ConstructResponse(Exception e);

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, message) = ConstructResponse(exception);
            
            context.Response.StatusCode = (int) statusCode;
            await context.Response.WriteAsJsonAsync(message);

        }
    }
    
}