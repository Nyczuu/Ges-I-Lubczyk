using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Misc.ProductionLabels.Infrastructure;

/// <summary>
/// Represents the plugin dependency registration - the sanctioned INopStartup mechanism (rule 2 of
/// Docs/ai-harness/00-system-instructions.md), auto-discovered by ITypeFinder, mirroring
/// Nop.Plugin.Misc.Ingredients/Infrastructure/NopStartup.cs and
/// Nop.Plugin.Misc.ServingSuggestions/Infrastructure/NopStartup.cs exactly. Not a Program.cs/Startup.cs
/// bare registration.
/// </summary>
public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        PluginServiceRegistrar.RegisterServices(services);
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 3000;

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }
}
