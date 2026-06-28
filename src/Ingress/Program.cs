var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "Ingress", status = "healthy" }));

app.MapGet("/", () => Results.Ok(new
{
    service = "Ingress",
    routes = new[] { "/api/ingest", "/api/reports", "/api/notifications", "/hubs/alarms" }
}));

app.MapMethods("/api/ingest/{**path}", new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo("IngestionService"));
app.MapMethods("/api/reports/{**path}", new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo("ReportingService"));
app.MapMethods("/api/notifications/{**path}", new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo("NotificationService"));
app.MapMethods("/hubs/alarms/{**path}", new[] { "GET", "POST", "OPTIONS" }, ProxyTo("NotificationService"));

app.Run();

static RequestDelegate ProxyTo(string targetConfigKey)
{
    return async context =>
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Ingress.Proxy");
        var targetBaseUrl = configuration[$"Routes:{targetConfigKey}"];

        if (string.IsNullOrWhiteSpace(targetBaseUrl))
        {
            logger.LogWarning("No target configured for {TargetConfigKey}", targetConfigKey);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync($"No target configured for {targetConfigKey}");
            return;
        }

        var targetUri = new Uri(new Uri(targetBaseUrl), context.Request.Path + context.Request.QueryString);
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri)
        {
            Content = context.Request.ContentLength > 0
                ? new StreamContent(context.Request.Body)
                : null
        };

        foreach (var header in context.Request.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        logger.LogInformation("Proxying {Method} {Path} to {TargetUri}", context.Request.Method, context.Request.Path, targetUri);
        using var response = await httpClientFactory.CreateClient().SendAsync(request, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    };
}
