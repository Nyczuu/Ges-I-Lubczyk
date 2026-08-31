using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents an ingredient service
/// </summary>
public class IngredientService : IIngredientService
{
    #region Fields

    protected readonly IIngredientCompositionService _ingredientCompositionService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedEntityService _localizedEntityService;
    protected readonly INopDataProvider _dataProvider;
    protected readonly IRepository<Ingredient> _ingredientRepository;
    protected readonly IRepository<IngredientComposition> _ingredientCompositionRepository;
    protected readonly IRepository<Product> _productRepository;
    protected readonly IRepository<ProductIngredientMapping> _productIngredientMappingRepository;

    #endregion

    #region Ctor

    public IngredientService(IIngredientCompositionService ingredientCompositionService,
        ILocalizationService localizationService,
        ILocalizedEntityService localizedEntityService,
        INopDataProvider dataProvider,
        IRepository<Ingredient> ingredientRepository,
        IRepository<IngredientComposition> ingredientCompositionRepository,
        IRepository<Product> productRepository,
        IRepository<ProductIngredientMapping> productIngredientMappingRepository)
    {
        _ingredientCompositionService = ingredientCompositionService;
        _localizationService = localizationService;
        _localizedEntityService = localizedEntityService;
        _dataProvider = dataProvider;
        _ingredientRepository = ingredientRepository;
        _ingredientCompositionRepository = ingredientCompositionRepository;
        _productRepository = productRepository;
        _productIngredientMappingRepository = productIngredientMappingRepository;
    }

    #endregion

    #region Utilities

    protected virtual async Task SaveLocalizedValuesAsync(Ingredient ingredient, IList<IngredientLocalizedValue> localizedValues)
    {
        if (localizedValues == null)
            return;

        foreach (var localized in localizedValues)
        {
            await _localizedEntityService.SaveLocalizedValueAsync(ingredient, x => x.Name, localized.Name, localized.LanguageId);
            await _localizedEntityService.SaveLocalizedValueAsync(ingredient, x => x.Description, localized.Description, localized.LanguageId);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets an ingredient by identifier
    /// </summary>
    public virtual async Task<Ingredient> GetIngredientByIdAsync(int ingredientId)
    {
        return await _ingredientRepository.GetByIdAsync(ingredientId, cache => default);
    }

    /// <summary>
    /// Gets ingredients by identifiers
    /// </summary>
    public virtual async Task<IList<Ingredient>> GetIngredientsByIdsAsync(int[] ingredientIds)
    {
        return await _ingredientRepository.GetByIdsAsync(ingredientIds);
    }

    /// <summary>
    /// Gets all ingredients
    /// </summary>
    public virtual async Task<IPagedList<Ingredient>> GetAllIngredientsAsync(string name = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _ingredientRepository.GetAllPagedAsync(query =>
        {
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(ingredient => ingredient.Name.Contains(name));

            query = query.OrderBy(ingredient => ingredient.Name);

            return query;
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Inserts an ingredient. Every ingredient always has a reflexive <see cref="IngredientClosure"/> row
    /// against itself at depth 0 (see the class doc on <see cref="IngredientClosure"/>) - seeded here via a
    /// full closure recompute, in the same transaction as the entity and localized-value writes.
    /// </summary>
    public virtual async Task InsertIngredientAsync(Ingredient ingredient, IList<IngredientLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        using var transaction = _dataProvider.CreateTransactionScope();

        ingredient.CreatedOnUtc = DateTime.UtcNow;
        ingredient.UpdatedOnUtc = DateTime.UtcNow;
        await _ingredientRepository.InsertAsync(ingredient);

        await SaveLocalizedValuesAsync(ingredient, localizedValues);

        await _ingredientCompositionService.RecomputeClosureAsync();

        transaction.Complete();
    }

    /// <summary>
    /// Updates an ingredient
    /// </summary>
    public virtual async Task UpdateIngredientAsync(Ingredient ingredient, IList<IngredientLocalizedValue> localizedValues = null)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        using var transaction = _dataProvider.CreateTransactionScope();

        ingredient.UpdatedOnUtc = DateTime.UtcNow;
        await _ingredientRepository.UpdateAsync(ingredient);

        await SaveLocalizedValuesAsync(ingredient, localizedValues);

        transaction.Complete();
    }

    /// <summary>
    /// Deletes an ingredient. Throws if the ingredient is still used as a component of another ingredient,
    /// or is still attached to a product; the message names what still uses it. No cascade.
    /// </summary>
    public virtual async Task DeleteIngredientAsync(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        var usedByIngredientIds = await _ingredientCompositionRepository.Table
            .Where(composition => composition.ChildIngredientId == ingredient.Id)
            .Select(composition => composition.ParentIngredientId)
            .Distinct()
            .ToListAsync();

        var usedByProductIds = await _productIngredientMappingRepository.Table
            .Where(mapping => mapping.IngredientId == ingredient.Id)
            .Select(mapping => mapping.ProductId)
            .Distinct()
            .ToListAsync();

        if (usedByIngredientIds.Any() || usedByProductIds.Any())
        {
            var messageParts = new List<string>();

            if (usedByIngredientIds.Any())
            {
                var names = (await _ingredientRepository.GetByIdsAsync(usedByIngredientIds)).Select(i => i.Name);
                messageParts.Add(string.Format(
                    await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.InUseByIngredients"),
                    string.Join(", ", names)));
            }

            if (usedByProductIds.Any())
            {
                var names = (await _productRepository.GetByIdsAsync(usedByProductIds)).Select(p => p.Name);
                messageParts.Add(string.Format(
                    await _localizationService.GetResourceAsync("Plugins.Misc.Ingredients.Errors.InUseByProducts"),
                    string.Join(", ", names)));
            }

            throw new NopException(string.Join(" ", messageParts));
        }

        using var transaction = _dataProvider.CreateTransactionScope();

        var ownOutgoingEdges = await _ingredientCompositionRepository.Table
            .Where(composition => composition.ParentIngredientId == ingredient.Id)
            .ToListAsync();

        if (ownOutgoingEdges.Any())
            await _ingredientCompositionRepository.DeleteAsync(ownOutgoingEdges);

        await _ingredientRepository.DeleteAsync(ingredient);

        await _ingredientCompositionService.RecomputeClosureAsync();

        transaction.Complete();
    }

    #endregion
}
