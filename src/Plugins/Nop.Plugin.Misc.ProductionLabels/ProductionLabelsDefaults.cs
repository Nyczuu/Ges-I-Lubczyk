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

    public static class Routes
    {
        private const string ROUTE_PREFIX = "Plugin.Misc.ProductionLabels.Route.";

        public static class Admin
        {
            public static string ListRouteName => ROUTE_PREFIX + "List";
        }
    }
}
