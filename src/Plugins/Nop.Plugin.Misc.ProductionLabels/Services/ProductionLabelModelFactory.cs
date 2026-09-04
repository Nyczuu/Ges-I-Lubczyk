using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Misc.Ingredients;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Plugin.Misc.ProductionLabels.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Npgsql;

namespace Nop.Plugin.Misc.ProductionLabels.Services;

/// <summary>
/// Represents a production label model factory
/// </summary>
public class ProductionLabelModelFactory : IProductionLabelModelFactory
{
    #region Fields

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IIngredientService _ingredientService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly IMeasureService _measureService;
    protected readonly IProductIngredientService _productIngredientService;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly MeasureSettings _measureSettings;

    #endregion

    #region Ctor

    public ProductionLabelModelFactory(IGenericAttributeService genericAttributeService,
        IIngredientService ingredientService,
        ILocalizationService localizationService,
        ILogger logger,
        IMeasureService measureService,
        IProductIngredientService productIngredientService,
        IProductService productService,
        IStoreContext storeContext,
        MeasureSettings measureSettings)
    {
        _genericAttributeService = genericAttributeService;
        _ingredientService = ingredientService;
        _localizationService = localizationService;
        _logger = logger;
        _measureService = measureService;
        _productIngredientService = productIngredientService;
        _productService = productService;
        _storeContext = storeContext;
        _measureSettings = measureSettings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Formats the net quantity with its unit, e.g. "0.25 kg" - empty when the product carries no weight
    /// </summary>
    protected virtual async Task<string> FormatNetQuantityAsync(Product product)
    {
        if (product.Weight <= decimal.Zero)
            return string.Empty;

        var measureWeight = await _measureService.GetMeasureWeightByIdAsync(_measureSettings.BaseWeightId);

        return $"{product.Weight:0.00} {measureWeight?.Name}".TrimEnd();
    }

    /// <summary>
    /// Reads the directly-attached ingredients and every composition edge reachable from them, via the
    /// exact same three public IIngredientService/IProductIngredientService calls the storefront widget
    /// already chains. Gracefully degrades to an empty result (plus a logged warning) when
    /// Nop.Plugin.Misc.Ingredients' own tables are missing - i.e. that plugin has been uninstalled while
    /// this one stays installed - scoped deliberately narrow to SQLSTATE 42P01 (undefined_table) so a
    /// genuine connection failure or an unrelated query bug still surfaces normally.
    /// </summary>
    protected virtual async Task<(IList<Ingredient> RootIngredients,
        IDictionary<int, IList<IngredientComposition>> ChildEdgesByParentId,
        IDictionary<int, Ingredient> IngredientsById)> ReadIngredientDataAsync(int productId)
    {
        try
        {
            var rootIngredients = await _productIngredientService.GetDirectIngredientsByProductIdAsync(productId);
            if (!rootIngredients.Any())
                return (new List<Ingredient>(), new Dictionary<int, IList<IngredientComposition>>(), new Dictionary<int, Ingredient>());

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

            return (rootIngredients, childEdgesByParentId, ingredientsById);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            await _logger.WarningAsync(
                "Nop.Plugin.Misc.ProductionLabels: could not read ingredient data (the Nop.Plugin.Misc.Ingredients " +
                "tables appear to be missing, i.e. that plugin has been uninstalled) - rendering the label without an ingredients section.",
                exception);

            return (new List<Ingredient>(), new Dictionary<int, IList<IngredientComposition>>(), new Dictionary<int, Ingredient>());
        }
    }

    /// <summary>
    /// Builds a nested label node for an ingredient, bounded to the maximum composition depth. Unlike the
    /// storefront widget's identically-shaped BuildNodeAsync (a cosmetic, silent cut-off), a node at the
    /// depth boundary that still has recorded child edges is real truncation on a real printed label - a
    /// compliance defect, not a display nicety - so this throws rather than silently rendering incomplete
    /// data. Confirmed unreachable for legitimately-entered data: IngredientCompositionService.ValidateNewEdgeAsync
    /// never allows a realized depth beyond IngredientsDefaults.MaxCompositionDepth.
    /// </summary>
    protected virtual async Task<ProductionLabelIngredientModel> BuildNodeAsync(Ingredient ingredient,
        IDictionary<int, IList<IngredientComposition>> childEdgesByParentId,
        IDictionary<int, Ingredient> ingredientsById,
        int languageId,
        int remainingDepth)
    {
        var node = new ProductionLabelIngredientModel
        {
            Name = await _localizationService.GetLocalizedAsync(ingredient, entity => entity.Name, languageId),
            AllergenType = ingredient.Allergen
        };

        if (!childEdgesByParentId.TryGetValue(ingredient.Id, out var childEdges))
            return node;

        if (remainingDepth <= 0)
        {
            throw new NopException(await _localizationService.GetResourceAsync(
                "Plugins.Misc.ProductionLabels.Errors.CompositionTruncated"));
        }

        foreach (var edge in childEdges)
        {
            if (!ingredientsById.TryGetValue(edge.ChildIngredientId, out var child))
                continue;

            node.Children.Add(await BuildNodeAsync(child, childEdgesByParentId, ingredientsById, languageId, remainingDepth - 1));
        }

        return node;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepares the label content for one product+batch, in the given language
    /// </summary>
    public virtual async Task<ProductionLabelModel> PrepareProductionLabelModelAsync(ProductionBatch productionBatch, int languageId, ProductionLabelSizeVariant sizeVariant)
    {
        ArgumentNullException.ThrowIfNull(productionBatch);

        var product = await _productService.GetProductByIdAsync(productionBatch.ProductId);
        var store = await _storeContext.GetCurrentStoreAsync();

        var model = new ProductionLabelModel
        {
            ProductName = await _localizationService.GetLocalizedAsync(product, entity => entity.Name, languageId),
            NetQuantity = await FormatNetQuantityAsync(product),
            BatchCode = productionBatch.BatchCode,
            BestBeforeDateUtc = productionBatch.BestBeforeDateUtc,
            CompanyName = store.CompanyName,
            CompanyAddress = store.CompanyAddress,
            CompanyPhoneNumber = store.CompanyPhoneNumber,
            StorageConditions = await _genericAttributeService.GetAttributeAsync<string>(product,
                ProductionLabelsDefaults.StorageConditionsAttributeKeyPrefix + languageId),
            CountryOfOrigin = await _genericAttributeService.GetAttributeAsync<string>(product,
                ProductionLabelsDefaults.CountryOfOriginAttributeKeyPrefix + languageId),
            SizeVariant = sizeVariant
        };

        var (rootIngredients, childEdgesByParentId, ingredientsById) = await ReadIngredientDataAsync(product.Id);

        foreach (var rootIngredient in rootIngredients)
        {
            model.Ingredients.Add(await BuildNodeAsync(rootIngredient, childEdgesByParentId, ingredientsById,
                languageId, IngredientsDefaults.MaxCompositionDepth));
        }

        return model;
    }

    #endregion
}
