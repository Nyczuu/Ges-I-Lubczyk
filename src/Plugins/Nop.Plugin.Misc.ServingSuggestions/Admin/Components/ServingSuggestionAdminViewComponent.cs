using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Factories;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Security;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ServingSuggestions.Admin.Components;

/// <summary>
/// Represents a view component that renders the "Serving suggestion" tab on the admin product-edit page
/// </summary>
public class ServingSuggestionAdminViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IPermissionService _permissionService;
    protected readonly IServingSuggestionService _servingSuggestionService;
    protected readonly ServingSuggestionAdminModelFactory _servingSuggestionAdminModelFactory;

    #endregion

    #region Ctor

    public ServingSuggestionAdminViewComponent(IPermissionService permissionService,
        IServingSuggestionService servingSuggestionService,
        ServingSuggestionAdminModelFactory servingSuggestionAdminModelFactory)
    {
        _permissionService = permissionService;
        _servingSuggestionService = servingSuggestionService;
        _servingSuggestionAdminModelFactory = servingSuggestionAdminModelFactory;
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

        if (!await _permissionService.AuthorizeAsync(ServingSuggestionsPermissionConfigManager.SERVING_SUGGESTIONS_VIEW))
            return Content(string.Empty);

        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(entityModel.Id);

        var model = await _servingSuggestionAdminModelFactory.PrepareServingSuggestionModelAsync(null, servingSuggestion, entityModel.Id);

        return View("~/Plugins/Misc.ServingSuggestions/Admin/Views/Components/ServingSuggestion.cshtml", model);
    }

    #endregion
}
