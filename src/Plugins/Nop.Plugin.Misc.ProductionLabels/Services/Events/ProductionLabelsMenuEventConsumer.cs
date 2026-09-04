using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.ProductionLabels.Services.Events;

/// <summary>
/// Represents the admin menu event consumer that adds the standalone "Production" entry as a sibling of
/// "Filter level values" inside the existing "Catalog" node - not anchored off Ingredients' own menu item,
/// because a missing anchor silently drops the item, and anchoring off Ingredients would hide "Production"
/// from an admin who has ProductionLabels.View but lacks Ingredients.View, or after an Ingredients uninstall
/// </summary>
public class ProductionLabelsMenuEventConsumer : BaseAdminMenuCreatedEventConsumer
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public ProductionLabelsMenuEventConsumer(ILocalizationService localizationService,
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
        return await _permissionService.AuthorizeAsync(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW);
    }

    protected override async Task<AdminMenuItem> GetAdminMenuItemAsync(IPlugin plugin)
    {
        return new AdminMenuItem
        {
            SystemName = ProductionLabelsDefaults.ProductionLabelsMenuSystemName,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.ProductionLabels.Production"),
            IconClass = "far fa-dot-circle",
            Url = _nopUrlHelper.RouteUrl(ProductionLabelsDefaults.Routes.Admin.ListRouteName),
            PermissionNames = new List<string> { ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW }
        };
    }

    #endregion

    #region Properties

    protected override string PluginSystemName => ProductionLabelsDefaults.SystemName;

    protected override MenuItemInsertType InsertType => MenuItemInsertType.After;

    protected override string AfterMenuSystemName => "Filter level values";

    #endregion
}
