using System.Net;
using ContainerRunner.Exceptions;
using ContainerRunner.Models;
using Newtonsoft.Json;

namespace ContainerRunner.Middleware;

public class UserExceptionHandlerMiddleware : AbstractExceptionHandlerMiddleware
{
    public UserExceptionHandlerMiddleware(RequestDelegate next) : base(next)
    {
    }

    public override (HttpStatusCode, Reason) ConstructResponse(Exception e)
    {
        HttpStatusCode code;
        switch (e)
        {
            case ImageNotFoundException
                or ContainerNotFoundException:
                code = HttpStatusCode.NotFound;
                break;
            default:
                code = HttpStatusCode.InternalServerError;
                break;
        }

        return (code, new Reason(e.Message));
    }
}