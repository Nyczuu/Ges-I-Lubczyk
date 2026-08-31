using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents a product ingredient service
/// </summary>
public class ProductIngredientService : IProductIngredientService
{
    #region Fields

    protected readonly IRepository<Ingredient> _ingredientRepository;
    protected readonly IRepository<IngredientClosure> _ingredientClosureRepository;
    protected readonly IRepository<IngredientComposition> _ingredientCompositionRepository;
    protected readonly IRepository<ProductIngredientMapping> _productIngredientMappingRepository;

    #endregion

    #region Ctor

    public ProductIngredientService(IRepository<Ingredient> ingredientRepository,
        IRepository<IngredientClosure> ingredientClosureRepository,
        IRepository<IngredientComposition> ingredientCompositionRepository,
        IRepository<ProductIngredientMapping> productIngredientMappingRepository)
    {
        _ingredientRepository = ingredientRepository;
        _ingredientClosureRepository = ingredientClosureRepository;
        _ingredientCompositionRepository = ingredientCompositionRepository;
        _productIngredientMappingRepository = productIngredientMappingRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the ingredient mappings of a product
    /// </summary>
    public virtual async Task<IPagedList<ProductIngredientMapping>> GetProductIngredientsByProductIdAsync(int productId,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _productIngredientMappingRepository.Table
            .Where(mapping => mapping.ProductId == productId)
            .OrderBy(mapping => mapping.DisplayOrder)
            .ThenBy(mapping => mapping.Id);

        return await query.ToPagedListAsync(pageIndex, pageSize);
    }

    /// <summary>
    /// Gets a product ingredient mapping by identifier
    /// </summary>
    public virtual async Task<ProductIngredientMapping> GetProductIngredientByIdAsync(int productIngredientMappingId)
    {
        return await _productIngredientMappingRepository.GetByIdAsync(productIngredientMappingId, cache => default);
    }

    /// <summary>
    /// Inserts a product ingredient mapping. Uniqueness of (ProductId, IngredientId) is enforced here with
    /// a check-then-insert, matching the precedent of ProductController's
    /// RelatedProductAddPopup/FilterLevelValuesAddPopup: if the mapping already exists, this is a silent
    /// no-op rather than a duplicate row.
    /// </summary>
    public virtual async Task InsertProductIngredientAsync(ProductIngredientMapping productIngredientMapping)
    {
        ArgumentNullException.ThrowIfNull(productIngredientMapping);

        var alreadyExists = await _productIngredientMappingRepository.Table
            .AnyAsync(mapping => mapping.ProductId == productIngredientMapping.ProductId
                && mapping.IngredientId == productIngredientMapping.IngredientId);

        if (alreadyExists)
            return;

        await _productIngredientMappingRepository.InsertAsync(productIngredientMapping);
    }

    /// <summary>
    /// Updates a product ingredient mapping
    /// </summary>
    public virtual async Task UpdateProductIngredientAsync(ProductIngredientMapping productIngredientMapping)
    {
        await _productIngredientMappingRepository.UpdateAsync(productIngredientMapping);
    }

    /// <summary>
    /// Deletes a product ingredient mapping
    /// </summary>
    public virtual async Task DeleteProductIngredientAsync(ProductIngredientMapping productIngredientMapping)
    {
        await _productIngredientMappingRepository.DeleteAsync(productIngredientMapping);
    }

    /// <summary>
    /// Gets the ingredients directly attached to a product (not the composition of those ingredients)
    /// </summary>
    public virtual async Task<IList<Ingredient>> GetDirectIngredientsByProductIdAsync(int productId)
    {
        var query = from mapping in _productIngredientMappingRepository.Table
                    join ingredient in _ingredientRepository.Table on mapping.IngredientId equals ingredient.Id
                    where mapping.ProductId == productId
                    orderby mapping.DisplayOrder, mapping.Id
                    select ingredient;

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets every composition edge reachable (at any depth) from the given root ingredients,
    /// via the ingredient closure
    /// </summary>
    public virtual async Task<IList<IngredientComposition>> GetCompositionsReachableFromAsync(IList<int> rootIngredientIds)
    {
        if (rootIngredientIds == null || !rootIngredientIds.Any())
            return new List<IngredientComposition>();

        //every ingredient reachable from a root, including the roots themselves via the reflexive closure rows
        var reachableIngredientIds = await _ingredientClosureRepository.Table
            .Where(closure => rootIngredientIds.Contains(closure.AncestorIngredientId))
            .Select(closure => closure.DescendantIngredientId)
            .Distinct()
            .ToListAsync();

        var query = _ingredientCompositionRepository.Table
            .Where(composition => reachableIngredientIds.Contains(composition.ParentIngredientId))
            .OrderBy(composition => composition.DisplayOrder)
            .ThenBy(composition => composition.Id);

        return await query.ToListAsync();
    }

    #endregion
}
