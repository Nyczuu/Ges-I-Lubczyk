using Nop.Core;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents a product ingredient service: attach/detach/reorder ingredients on a product,
/// and the storefront read path (directly-attached ingredients plus their reachable composition edges)
/// </summary>
public interface IProductIngredientService
{
    /// <summary>
    /// Gets the ingredient mappings of a product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of product ingredient mappings
    /// </returns>
    Task<IPagedList<ProductIngredientMapping>> GetProductIngredientsByProductIdAsync(int productId, int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets a product ingredient mapping by identifier
    /// </summary>
    /// <param name="productIngredientMappingId">Product ingredient mapping identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the product ingredient mapping
    /// </returns>
    Task<ProductIngredientMapping> GetProductIngredientByIdAsync(int productIngredientMappingId);

    /// <summary>
    /// Inserts a product ingredient mapping
    /// </summary>
    /// <param name="productIngredientMapping">Product ingredient mapping</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertProductIngredientAsync(ProductIngredientMapping productIngredientMapping);

    /// <summary>
    /// Updates a product ingredient mapping
    /// </summary>
    /// <param name="productIngredientMapping">Product ingredient mapping</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateProductIngredientAsync(ProductIngredientMapping productIngredientMapping);

    /// <summary>
    /// Deletes a product ingredient mapping
    /// </summary>
    /// <param name="productIngredientMapping">Product ingredient mapping</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteProductIngredientAsync(ProductIngredientMapping productIngredientMapping);

    /// <summary>
    /// Gets the ingredients directly attached to a product (not the composition of those ingredients)
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the directly-attached ingredients, ordered by display order
    /// </returns>
    Task<IList<Ingredient>> GetDirectIngredientsByProductIdAsync(int productId);

    /// <summary>
    /// Gets every composition edge reachable (at any depth) from the given root ingredients,
    /// via the ingredient closure. Used to render the full nested composition without a
    /// per-level recursive read.
    /// </summary>
    /// <param name="rootIngredientIds">Root ingredient identifiers</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the reachable composition edges
    /// </returns>
    Task<IList<IngredientComposition>> GetCompositionsReachableFromAsync(IList<int> rootIngredientIds);
}
