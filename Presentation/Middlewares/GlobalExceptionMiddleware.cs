using Application.Exceptions;
using System.Net;
using System.Text.Json;


namespace Presentation.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (ValidationException ex)
            {
                await WriteResponse(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (NotFoundException ex)
            {
                await WriteResponse(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (ConflictException ex)
            {
                await WriteResponse(context, HttpStatusCode.Conflict, ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                await WriteResponse(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (ExternalServiceUnavailableException ex)
            {
                await WriteResponse(context, HttpStatusCode.ServiceUnavailable, ex.Message);
            }
            catch (AppException ex)
            {
                // Fallback for any custom business exception that doesn't have a specific HTTP status
                await WriteResponse(context, HttpStatusCode.UnprocessableEntity, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                // TEMPORARY: Log full error details in development
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                var errorMessage = isDevelopment ? ex.ToString() : "An unexpected error occurred. Please try again later.";

                await WriteResponse(context, HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        private static Task WriteResponse(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = (int)statusCode,
                message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

}
