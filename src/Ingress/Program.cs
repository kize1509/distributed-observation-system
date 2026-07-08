var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("ingestion", client =>
    client.BaseAddress = new Uri(builder.Configuration["Routes__IngestionService"] ?? "http://ingestion:8080"));

builder.Services.AddHttpClient("notification", client =>
    client.BaseAddress = new Uri(builder.Configuration["Routes__NotificationService"] ?? "http://notification:8080"));

builder.Services.AddHttpClient("reporting", client =>
    client.BaseAddress = new Uri(builder.Configuration["Routes__ReportingService"] ?? "http://reporting:8080"));

var app = builder.Build();

var factory = app.Services.GetRequiredService<IHttpClientFactory>();
var logger  = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Ingress.Proxy");

var ingestion    = factory.CreateClient("ingestion");
var notification = factory.CreateClient("notification");
var reporting    = factory.CreateClient("reporting");

app.MapGet("/healthz", () => Results.Ok(new { service = "Ingress", status = "healthy" }));

app.MapGet("/", () => Results.Ok(new
{
    service = "Ingress",
    routes = new[] { "/api/ingest", "/api/reports", "/api/notifications", "/hubs/alarms" }
}));

HashSet<string> hopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
{
    "Transfer-Encoding", "Connection", "Keep-Alive", "TE", "Trailers", "Upgrade"
};

app.MapMethods("/api/ingest/{**path}",         new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo(ingestion));
app.MapMethods("/api/reports/{**path}",        new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo(reporting));
app.MapMethods("/api/notifications/{**path}",  new[] { "GET", "POST", "PUT", "DELETE" }, ProxyTo(notification));
app.MapMethods("/hubs/alarms/{**path}",        new[] { "GET", "POST", "DELETE", "OPTIONS" }, ProxyTo(notification));

app.Run();



RequestDelegate ProxyTo(HttpClient client) =>
    async context =>
    {
        var targetUri = new Uri(client.BaseAddress!, context.Request.Path + context.Request.QueryString);

        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri)
        {
            Content = context.Request.ContentLength is not 0
                ? new StreamContent(context.Request.Body)
                : null
        };

        foreach (var header in context.Request.Headers)
        {
            if (hopByHopHeaders.Contains(header.Key)) continue;
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        logger.LogInformation("Proxying {Method} {Path} → {Target}", context.Request.Method, context.Request.Path, targetUri);

        using var response = await client.SendAsync(request, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            if (hopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            if (hopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    };
