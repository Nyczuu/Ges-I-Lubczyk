using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Misc.ProductionLabels.Admin.Factories;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Plugin.Misc.ProductionLabels.Services.Pdf;

namespace Nop.Plugin.Misc.ProductionLabels.Infrastructure;

/// <summary>
/// Holds the actual per-service registration calls for <see cref="NopStartup.ConfigureServices"/>, kept in
/// a plain helper method (rather than inline in <c>NopStartup.ConfigureServices</c> itself) purely to keep
/// that method short; this is still reached exclusively through the sanctioned <c>INopStartup</c>
/// mechanism (rule 2 of <c>Docs/ai-harness/00-system-instructions.md</c>) - see
/// <c>Infrastructure/NopStartup.cs</c>, which is the only caller.
/// </summary>
internal static class PluginServiceRegistrar
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IProductionBatchService, ProductionBatchService>();
        services.AddScoped<IProductionLabelModelFactory, ProductionLabelModelFactory>();

        //the concrete HTML-to-PDF library choice (spec Section 13) is PuppeteerSharp, confirmed by a real
        //build-and-render smoke test against the Alpine-based runtime image - see
        //PuppeteerSharpHtmlToPdfConverter's own remarks for why it caches the launched browser in a
        //static field rather than tying it to this scoped registration
        services.AddScoped<IHtmlToPdfConverter, PuppeteerSharpHtmlToPdfConverter>();

        services.AddScoped<ProductionLabelsAdminModelFactory>();
    }
}
