using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Ingredients.Admin.Factories;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Security;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Admin.Components;

/// <summary>
/// Represents a view component that renders the "Ingredients" tab on the admin product-edit page
/// </summary>
public class ProductIngredientsAdminViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IngredientAdminModelFactory _ingredientAdminModelFactory;
    protected readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public ProductIngredientsAdminViewComponent(IngredientAdminModelFactory ingredientAdminModelFactory,
        IPermissionService permissionService)
    {
        _ingredientAdminModelFactory = ingredientAdminModelFactory;
        _permissionService = permissionService;
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

        if (!await _permissionService.AuthorizeAsync(IngredientsPermissionConfigManager.INGREDIENTS_VIEW))
            return Content(string.Empty);

        var searchModel = await _ingredientAdminModelFactory.PrepareProductIngredientSearchModelAsync(new ProductIngredientSearchModel
        {
            ProductId = entityModel.Id
        });

        return View("~/Plugins/Misc.Ingredients/Admin/Views/Components/ProductIngredients.cshtml", searchModel);
    }

    #endregion
}
