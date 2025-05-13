using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProjectManagementSystem1.Middleware
{
    public class JwtErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtErrorHandlingMiddleware> _logger;

        public JwtErrorHandlingMiddleware(RequestDelegate next, ILogger<JwtErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context); // Proceed to next middleware
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. ");
                context.Response.ContentType = "application/json";

                var statusCode = ex switch
                {
                    UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                    ArgumentException => (int)HttpStatusCode.BadRequest,
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                context.Response.StatusCode = statusCode;

                var errorResponse = new
                {
                    error = ex.GetType().Name,
                    message = ex.Message,
                    statusCode = statusCode
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        }

    }

    public static class JwtErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtErrorHandlingMiddleware>();
        }
    }
}﻿
