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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapProductEndpoints();
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
