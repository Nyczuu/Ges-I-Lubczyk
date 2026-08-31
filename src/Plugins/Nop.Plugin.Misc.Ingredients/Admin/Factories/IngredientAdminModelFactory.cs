using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.Ingredients.Admin.Models;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Localization;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.Ingredients.Admin.Factories;

/// <summary>
/// Represents the ingredient admin model factory
/// </summary>
public class IngredientAdminModelFactory
{
    #region Fields

    protected readonly IIngredientCompositionService _ingredientCompositionService;
    protected readonly IIngredientService _ingredientService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedModelFactory _localizedModelFactory;
    protected readonly IProductIngredientService _productIngredientService;

    #endregion

    #region Ctor

    public IngredientAdminModelFactory(IIngredientCompositionService ingredientCompositionService,
        IIngredientService ingredientService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IProductIngredientService productIngredientService)
    {
        _ingredientCompositionService = ingredientCompositionService;
        _ingredientService = ingredientService;
        _localizationService = localizationService;
        _localizedModelFactory = localizedModelFactory;
        _productIngredientService = productIngredientService;
    }

    #endregion

    #region Utilities

    protected virtual IngredientCompositionSearchModel PrepareIngredientCompositionSearchModel(IngredientCompositionSearchModel searchModel, Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        ArgumentNullException.ThrowIfNull(ingredient);

        searchModel.IngredientId = ingredient.Id;
        searchModel.SetGridPageSize();

        return searchModel;
    }

    protected virtual async Task PrepareAllergenTypesAsync(IList<SelectListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var allergenType in Enum.GetValues<AllergenType>())
        {
            items.Add(new SelectListItem
            {
                Text = await _localizationService.GetLocalizedEnumAsync(allergenType),
                Value = ((int)allergenType).ToString()
            });
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare ingredient search model
    /// </summary>
    public virtual Task<IngredientSearchModel> PrepareIngredientSearchModelAsync(IngredientSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepare paged ingredient list model
    /// </summary>
    public virtual async Task<IngredientListModel> PrepareIngredientListModelAsync(IngredientSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var ingredients = await _ingredientService.GetAllIngredientsAsync(searchModel.SearchName,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var model = await new IngredientListModel().PrepareToGridAsync(searchModel, ingredients, () =>
        {
            return ingredients.SelectAwait(async ingredient =>
            {
                var ingredientModel = ingredient.ToModel<IngredientModel>();

                return ingredientModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare ingredient model
    /// </summary>
    public virtual async Task<IngredientModel> PrepareIngredientModelAsync(IngredientModel model, Ingredient ingredient, bool excludeProperties = false)
    {
        Func<IngredientLocalizedModel, int, Task> localizedModelConfiguration = null;

        if (ingredient != null)
        {
            model ??= ingredient.ToModel<IngredientModel>();

            PrepareIngredientCompositionSearchModel(model.IngredientCompositionSearchModel, ingredient);

            localizedModelConfiguration = async (locale, languageId) =>
            {
                locale.Name = await _localizationService.GetLocalizedAsync(ingredient, entity => entity.Name, languageId, false, false);
                locale.Description = await _localizationService.GetLocalizedAsync(ingredient, entity => entity.Description, languageId, false, false);
            };
        }

        if (!excludeProperties)
            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

        await PrepareAllergenTypesAsync(model.AvailableAllergenTypes);

        return model;
    }

    /// <summary>
    /// Prepare paged ingredient composition list model (the grid of an ingredient's own children)
    /// </summary>
    public virtual async Task<IngredientCompositionListModel> PrepareIngredientCompositionListModelAsync(IngredientCompositionSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var compositions = (await _ingredientCompositionService.GetChildCompositionsAsync(searchModel.IngredientId))
            .ToPagedList(searchModel);

        var childIngredientIds = compositions.Select(composition => composition.ChildIngredientId).Distinct().ToArray();
        var childIngredients = await _ingredientService.GetIngredientsByIdsAsync(childIngredientIds);

        var model = await new IngredientCompositionListModel().PrepareToGridAsync(searchModel, compositions, () =>
        {
            return compositions.SelectAwait(async composition =>
            {
                var compositionModel = composition.ToModel<IngredientCompositionModel>();

                compositionModel.ChildIngredientName = childIngredients
                    .FirstOrDefault(childIngredient => childIngredient.Id == composition.ChildIngredientId)?.Name;

                return compositionModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare product ingredient search model
    /// </summary>
    public virtual Task<ProductIngredientSearchModel> PrepareProductIngredientSearchModelAsync(ProductIngredientSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepare paged product ingredient list model (the grid on the product-edit page tab)
    /// </summary>
    public virtual async Task<ProductIngredientListModel> PrepareProductIngredientListModelAsync(ProductIngredientSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var productIngredients = await _productIngredientService.GetProductIngredientsByProductIdAsync(searchModel.ProductId,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var ingredientIds = productIngredients.Select(mapping => mapping.IngredientId).Distinct().ToArray();
        var ingredients = await _ingredientService.GetIngredientsByIdsAsync(ingredientIds);

        var model = await new ProductIngredientListModel().PrepareToGridAsync(searchModel, productIngredients, () =>
        {
            return productIngredients.SelectAwait(async mapping =>
            {
                var mappingModel = mapping.ToModel<ProductIngredientModel>();

                mappingModel.IngredientName = ingredients
                    .FirstOrDefault(ingredient => ingredient.Id == mapping.IngredientId)?.Name;

                return mappingModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare ingredient search model to add ingredients from a popup (either to a composite ingredient,
    /// or to a product)
    /// </summary>
    public virtual Task<IngredientSearchModel> PrepareAddIngredientSearchModelAsync(IngredientSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.SetPopupGridPageSize();

        return Task.FromResult(searchModel);
    }

    #endregion
}
