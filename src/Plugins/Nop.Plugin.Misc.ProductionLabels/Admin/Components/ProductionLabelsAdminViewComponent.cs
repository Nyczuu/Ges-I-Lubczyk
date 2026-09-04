using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.ProductionLabels.Admin.Factories;
using Nop.Plugin.Misc.ProductionLabels.Services;
using Nop.Services.Security;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ProductionLabels.Admin.Components;

/// <summary>
/// Represents a view component that renders the "Production" tab on the admin product-edit page. Only one
/// zone is ever passed to this plugin (<see cref="AdminWidgetZones.ProductDetailsBlock"/> - no
/// storefront zone at all), so unlike Ingredients/ServingSuggestions' own admin view components there is
/// no zone switch needed
/// </summary>
public class ProductionLabelsAdminViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IPermissionService _permissionService;
    protected readonly ProductionLabelsAdminModelFactory _productionLabelsAdminModelFactory;

    #endregion

    #region Ctor

    public ProductionLabelsAdminViewComponent(IPermissionService permissionService,
        ProductionLabelsAdminModelFactory productionLabelsAdminModelFactory)
    {
        _permissionService = permissionService;
        _productionLabelsAdminModelFactory = productionLabelsAdminModelFactory;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke the widget view component
    /// </summary>
    /// <param name="widgetZone">Widget zone</param>
    /// <param name="additionalData">Additional parameters</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the view component result
    /// </returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (!widgetZone.Equals(AdminWidgetZones.ProductDetailsBlock))
            return Content(string.Empty);

        if (additionalData is not BaseNopEntityModel entityModel)
            return Content(string.Empty);

        if (!await _permissionService.AuthorizeAsync(ProductionLabelsPermissionConfigManager.PRODUCTION_LABELS_VIEW))
            return Content(string.Empty);

        var model = await _productionLabelsAdminModelFactory.PrepareProductionLabelsProductModelAsync(entityModel.Id);

        return View("~/Plugins/Misc.ProductionLabels/Admin/Views/Components/ProductionLabels.cshtml", model);
    }

    #endregion
}
