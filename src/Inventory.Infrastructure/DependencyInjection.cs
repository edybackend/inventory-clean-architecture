using Inventory.Application.Abstractions;
using Inventory.Application.Queries;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Inventory") ?? "Data Source=data/inventory.db";

        services.AddDbContext<InventoryDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductQueries, EfProductQueries>();

        return services;
    }
}
