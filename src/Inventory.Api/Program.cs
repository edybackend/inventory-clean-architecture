using System.Text.Json.Serialization;
using Inventory.Api.Endpoints;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

await app.Services.InitializeInventoryDatabaseAsync(app.Configuration.GetValue("Inventory:SeedSampleData", false));

// Malformed JSON (e.g. an unknown movement type) raises BadHttpRequestException, which carries its own 400.
// Without the selector the exception handler would turn it into a 500 in Development.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapProductEndpoints();
// Unknown /api routes are 404 problems; every other unknown path is an Angular route and gets index.html.
app.MapFallback("/api/{**path}", () => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
