using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.Ingredients.Services.Events;

/// <summary>
/// Represents the admin menu event consumer that adds the Ingredients entry as a sibling
/// of "Filter level values"/"Product tags" inside the existing "Catalog" node
/// </summary>
public class IngredientsMenuEventConsumer : BaseAdminMenuCreatedEventConsumer
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public IngredientsMenuEventConsumer(ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        IPluginManager<IPlugin> pluginManager) : base(pluginManager)
    {
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
    }

    #endregion

    #region Utilities

    protected override async Task<bool> CheckAccessAsync()
    {
        return await _permissionService.AuthorizeAsync(IngredientsPermissionConfigManager.INGREDIENTS_VIEW);
    }

    protected override async Task<AdminMenuItem> GetAdminMenuItemAsync(IPlugin plugin)
    {
        return new AdminMenuItem
        {
            SystemName = IngredientsDefaults.IngredientsMenuSystemName,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Ingredients"),
            IconClass = "far fa-dot-circle",
            Url = _nopUrlHelper.RouteUrl(IngredientsDefaults.Routes.Admin.ListRouteName),
            PermissionNames = new List<string> { IngredientsPermissionConfigManager.INGREDIENTS_VIEW }
        };
    }

    #endregion

    #region Properties

    protected override string PluginSystemName => IngredientsDefaults.SystemName;

    protected override MenuItemInsertType InsertType => MenuItemInsertType.After;

    protected override string AfterMenuSystemName => "Filter level values";

    #endregion
}
