namespace Nop.Plugin.Misc.Ingredients;

/// <summary>
/// Represents plugin constants
/// </summary>
public class IngredientsDefaults
{
    /// <summary>
    /// Gets a plugin system name
    /// </summary>
    public static string SystemName => "Misc.Ingredients";

    /// <summary>
    /// Gets the ingredients administration menu system name
    /// </summary>
    public static string IngredientsMenuSystemName => "Ingredients";

    /// <summary>
    /// Gets the maximum allowed composition depth (ingredient-to-ingredient edges only)
    /// </summary>
    public static int MaxCompositionDepth => 3;

    public static class Routes
    {
        private const string ROUTE_PREFIX = "Plugin.Misc.Ingredients.Route.";

        public static class Admin
        {
            public static string ListRouteName => ROUTE_PREFIX + "List";
        }
    }
}
