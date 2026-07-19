using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PolicyPlatform.Domain.Assistance;
using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Api.Filters;

/// <summary>Maps assistance-reports validation failures to the contract's error shape
/// ({ code, message }) and status codes, so controllers don't repeat the mapping.</summary>
public sealed class AssistanceValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case AssistanceDomainException ex:
                context.Result = ErrorResult(ex.Code, ex.Message);
                context.ExceptionHandled = true;
                break;
            case UnauthorizedAccessException ex:
                context.Result = ErrorResult(AssistanceErrorCodes.Unauthorized, ex.Message, StatusCodes.Status401Unauthorized);
                context.ExceptionHandled = true;
                break;
            case DomainException ex:
                context.Result = new ObjectResult(new { message = ex.Message })
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                };
                context.ExceptionHandled = true;
                break;
        }
    }

    private static ObjectResult ErrorResult(string code, string message, int? statusCode = null)
    {
        var status = statusCode ?? code switch
        {
            AssistanceErrorCodes.DuplicateSubmission => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return new ObjectResult(new { code, message }) { StatusCode = status };
    }
}
