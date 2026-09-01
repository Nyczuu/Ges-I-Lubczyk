using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.ServingSuggestions.Public.Models;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.ServingSuggestions.Public.Components;

/// <summary>
/// Represents a view component that renders a product's serving suggestion (title, description, image,
/// ordered steps)
/// </summary>
public class ServingSuggestionViewComponent : NopViewComponent
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IServingSuggestionService _servingSuggestionService;

    #endregion

    #region Ctor

    public ServingSuggestionViewComponent(ILocalizationService localizationService,
        IPictureService pictureService,
        IServingSuggestionService servingSuggestionService)
    {
        _localizationService = localizationService;
        _pictureService = pictureService;
        _servingSuggestionService = servingSuggestionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Builds the storefront serving suggestion model for a product - extracted from
    /// <see cref="InvokeAsync"/> so the rendering logic is testable without a
    /// <see cref="Microsoft.AspNetCore.Mvc.ViewComponentContext"/>. Returns null when the product has no
    /// serving suggestion.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the serving suggestion model, or null
    /// </returns>
    public virtual async Task<PublicServingSuggestionModel> PrepareServingSuggestionModelAsync(int productId)
    {
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(productId);
        if (servingSuggestion == null)
            return null;

        var model = new PublicServingSuggestionModel
        {
            Title = await _localizationService.GetLocalizedAsync(servingSuggestion, entity => entity.Title),
            Description = await _localizationService.GetLocalizedAsync(servingSuggestion, entity => entity.Description),
            PictureUrl = await _pictureService.GetPictureUrlAsync(servingSuggestion.PictureId)
        };

        var steps = await _servingSuggestionService.GetServingSuggestionStepsAsync(servingSuggestion.Id);
        foreach (var step in steps)
        {
            model.Steps.Add(new PublicServingSuggestionStepModel
            {
                Text = await _localizationService.GetLocalizedAsync(step, entity => entity.Text)
            });
        }

        return model;
    }

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
        //no separate ACL check: this inherits whatever visibility the product page itself already enforced
        if (additionalData is not BaseNopEntityModel entityModel)
            return Content(string.Empty);

        var model = await PrepareServingSuggestionModelAsync(entityModel.Id);
        if (model == null)
            return Content(string.Empty);

        return View("~/Plugins/Misc.ServingSuggestions/Public/Views/Components/ServingSuggestion.cshtml", model);
    }

    #endregion
}
