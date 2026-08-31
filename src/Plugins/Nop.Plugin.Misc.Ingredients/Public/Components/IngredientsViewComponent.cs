using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Public.Models;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.Ingredients.Public.Components;

/// <summary>
/// Represents a view component that renders a product's full ingredient composition,
/// fully expanded inline, EU-label style (e.g. "beef broth (bones, water, carrot, celery, salt)")
/// </summary>
public class IngredientsViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IIngredientService _ingredientService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IProductIngredientService _productIngredientService;

    #endregion

    #region Ctor

    public IngredientsViewComponent(IIngredientService ingredientService,
        ILocalizationService localizationService,
        IProductIngredientService productIngredientService)
    {
        _ingredientService = ingredientService;
        _localizationService = localizationService;
        _productIngredientService = productIngredientService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Builds a nested node for an ingredient, bounded to the maximum composition depth
    /// as a defensive render-time cut-off against bad data
    /// </summary>
    protected virtual async Task<PublicIngredientModel> BuildNodeAsync(Ingredient ingredient,
        IDictionary<int, IList<IngredientComposition>> childEdgesByParentId,
        IDictionary<int, Ingredient> ingredientsById,
        int remainingDepth)
    {
        var node = new PublicIngredientModel
        {
            Name = await _localizationService.GetLocalizedAsync(ingredient, entity => entity.Name)
        };

        if (remainingDepth <= 0 || !childEdgesByParentId.TryGetValue(ingredient.Id, out var childEdges))
            return node;

        foreach (var edge in childEdges)
        {
            if (!ingredientsById.TryGetValue(edge.ChildIngredientId, out var child))
                continue;

            node.Children.Add(await BuildNodeAsync(child, childEdgesByParentId, ingredientsById, remainingDepth - 1));
        }

        return node;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Builds the storefront ingredients model for a product: its directly-attached ingredients, each with
    /// its own (possibly nested) composition, bounded to the maximum composition depth as a defensive
    /// render-time cut-off. Extracted from <see cref="InvokeAsync"/> so the actual rendering logic is
    /// testable without a <see cref="Microsoft.AspNetCore.Mvc.ViewComponentContext"/> - returns an empty
    /// model (no ingredients) when the product has none directly attached.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ingredients model
    /// </returns>
    public virtual async Task<IngredientsModel> PrepareIngredientsModelAsync(int productId)
    {
        var model = new IngredientsModel();

        var rootIngredients = await _productIngredientService.GetDirectIngredientsByProductIdAsync(productId);
        if (!rootIngredients.Any())
            return model;

        //query 2: every composition edge reachable from the directly-attached ingredients, at any depth
        var rootIngredientIds = rootIngredients.Select(ingredient => ingredient.Id).ToList();
        var reachableEdges = await _productIngredientService.GetCompositionsReachableFromAsync(rootIngredientIds);

        var allInvolvedIngredientIds = rootIngredientIds
            .Concat(reachableEdges.Select(edge => edge.ParentIngredientId))
            .Concat(reachableEdges.Select(edge => edge.ChildIngredientId))
            .Distinct()
            .ToArray();

        var ingredientsById = (await _ingredientService.GetIngredientsByIdsAsync(allInvolvedIngredientIds))
            .ToDictionary(ingredient => ingredient.Id);

        var childEdgesByParentId = reachableEdges
            .GroupBy(edge => edge.ParentIngredientId)
            .ToDictionary(group => group.Key, group => (IList<IngredientComposition>)group.ToList());

        foreach (var rootIngredient in rootIngredients)
        {
            model.Ingredients.Add(await BuildNodeAsync(rootIngredient, childEdgesByParentId, ingredientsById,
                IngredientsDefaults.MaxCompositionDepth));
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

        var model = await PrepareIngredientsModelAsync(entityModel.Id);
        if (!model.Ingredients.Any())
            return Content(string.Empty);

        return View("~/Plugins/Misc.Ingredients/Public/Views/Components/Ingredients.cshtml", model);
    }

    #endregion
}
