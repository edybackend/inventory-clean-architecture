using FluentValidation;
using Inventory.Application.Movements;
using Inventory.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<RegisterProductValidator>(ServiceLifetime.Singleton);
        services.AddScoped<RegisterProductHandler>();
        services.AddScoped<RecordMovementHandler>();
        return services;
    }
}
