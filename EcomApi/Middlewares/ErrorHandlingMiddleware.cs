using System.Text.Json;

namespace EcomApi.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next; _logger = logger;
        }

        public async Task Invoke(HttpContext ctx)
        {
            try { await _next(ctx); }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = ex is ApplicationException
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;

                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = ex.Message
                }));
            }
        }
    }
}
