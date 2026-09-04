namespace Nop.Plugin.Misc.ProductionLabels;

/// <summary>
/// Represents plugin constants
/// </summary>
public class ProductionLabelsDefaults
{
    /// <summary>
    /// Gets a plugin system name
    /// </summary>
    public static string SystemName => "Misc.ProductionLabels";

    /// <summary>
    /// Gets the production labels administration menu system name
    /// </summary>
    public static string ProductionLabelsMenuSystemName => "Production";

    /// <summary>
    /// Gets the key prefix of the per-(product, language) storage conditions <see cref="Nop.Core.Domain.Common.GenericAttribute"/>;
    /// the language identifier is appended at call sites (e.g. "ProductionLabels.StorageConditions.1")
    /// </summary>
    public static string StorageConditionsAttributeKeyPrefix => "ProductionLabels.StorageConditions.";

    /// <summary>
    /// Gets the key prefix of the per-(product, language) country of origin <see cref="Nop.Core.Domain.Common.GenericAttribute"/>;
    /// the language identifier is appended at call sites (e.g. "ProductionLabels.CountryOfOrigin.1")
    /// </summary>
    public static string CountryOfOriginAttributeKeyPrefix => "ProductionLabels.CountryOfOrigin.";

    /// <summary>
    /// Gets the name of the environment variable that, when set, points
    /// <see cref="Services.Pdf.PuppeteerSharpHtmlToPdfConverter"/> at a system-installed Chromium
    /// executable instead of letting PuppeteerSharp download its own build. Always set in the runtime
    /// Docker image (Alpine's own <c>chromium</c> apk package, at <c>/usr/bin/chromium-browser</c>) -
    /// PuppeteerSharp's bundled downloader fetches a glibc build that does not run on musl/Alpine.
    /// Left unset on a developer machine so PuppeteerSharp can download a compatible build itself.
    /// </summary>
    public static string ChromiumExecutablePathEnvironmentVariable => "PRODUCTIONLABELS_CHROMIUM_EXECUTABLE_PATH";

    public static class Routes
    {
        private const string ROUTE_PREFIX = "Plugin.Misc.ProductionLabels.Route.";

        public static class Admin
        {
            public static string ListRouteName => ROUTE_PREFIX + "List";
        }
    }
}
