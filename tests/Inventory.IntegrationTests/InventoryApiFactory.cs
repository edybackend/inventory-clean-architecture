using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Inventory.IntegrationTests;

/// <summary>Runs the real API, EF Core and SQLite against a throw-away database file.</summary>
public class InventoryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"inventory-tests-{Guid.NewGuid():N}.db");

    protected virtual bool SeedSampleData => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Inventory", $"Data Source={_databasePath};Pooling=False");
        builder.UseSetting("Inventory:SeedSampleData", SeedSampleData.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_databasePath);
    }
}

public sealed class SeededInventoryApiFactory : InventoryApiFactory
{
    protected override bool SeedSampleData => true;
}
