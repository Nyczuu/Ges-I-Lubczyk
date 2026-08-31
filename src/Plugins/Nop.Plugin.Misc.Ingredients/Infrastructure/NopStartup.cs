using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.Ingredients.Admin.Factories;
using Nop.Plugin.Misc.Ingredients.Services;

namespace Nop.Plugin.Misc.Ingredients.Infrastructure;

/// <summary>
/// Represents the plugin dependency registration
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<IIngredientCompositionService, IngredientCompositionService>();
        services.AddScoped<IProductIngredientService, ProductIngredientService>();

        services.AddScoped<IngredientAdminModelFactory>();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 3000;
}
