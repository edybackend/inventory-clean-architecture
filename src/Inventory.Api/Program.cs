var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => TypedResults.Ok());

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
