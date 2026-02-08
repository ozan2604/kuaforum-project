using KuaforumAPI.Application.Exceptions;
using KuaforumAPI.Application.Models;
using System.Net;
using FluentValidation;
using ValidationException = KuaforumAPI.Application.Exceptions.ValidationException;

namespace KuaforumAPI.WebAPI.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var message = "Internal Server Error";
            int statusCode = (int)HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case NotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;
                case KuaforumAPI.Application.Exceptions.ValidationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;
                case FluentValidation.ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage));
                    break;

                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = "Invalid credentials";
                    break;
                // Add more custom exceptions here
                default:
                    message = exception.Message; // In production, might want to hide this
                    break;
            }

            context.Response.StatusCode = statusCode;

            var errorDetails = new ErrorDetails
            {
                StatusCode = statusCode,
                Message = message
            };

            await context.Response.WriteAsync(errorDetails.ToString());
        }
    }
}
