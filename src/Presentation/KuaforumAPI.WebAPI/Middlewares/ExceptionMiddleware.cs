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
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
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
                default:
                    message = _env.IsProduction() ? "Internal Server Error" : exception.Message;
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
