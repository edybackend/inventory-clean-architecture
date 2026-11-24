using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Creates the SQLite folder if needed, applies pending migrations and, optionally, loads sample data into an empty database.</summary>
    public static async Task InitializeInventoryDatabaseAsync(this IServiceProvider services, bool seedSampleData, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var dataSource = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory) && dataSource != ":memory:")
        {
            Directory.CreateDirectory(directory);
        }

        await db.Database.MigrateAsync(cancellationToken);

        if (seedSampleData && !await db.Products.AnyAsync(cancellationToken))
        {
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            db.Products.AddRange(SampleData.Create(clock.GetUtcNow(), out var movements));
            db.Movements.AddRange(movements);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
