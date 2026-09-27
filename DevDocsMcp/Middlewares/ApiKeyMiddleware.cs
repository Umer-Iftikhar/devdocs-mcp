namespace DevDocsMcp.Middlewares
{
    public class ApiKeyMiddleware
    {
        private const string HeaderName = "X-API-Key";
        private readonly RequestDelegate _next;
        private readonly string _expectedKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _expectedKey = config["Auth:ApiKey"]
                ?? throw new InvalidOperationException("Auth:ApiKey is not configured.");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Read the key from the request header
            if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key missing.");
                return;
            }

            // 2 + 3. Constant-time compare against the expected key
            var providedBytes = System.Text.Encoding.UTF8.GetBytes(providedKey!);
            var expectedBytes = System.Text.Encoding.UTF8.GetBytes(_expectedKey);

            if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
            {
                // 4. No match -> 401
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key.");
                return;
            }

            // Match -> continue down the pipeline
            await _next(context);
        }
    }
}
