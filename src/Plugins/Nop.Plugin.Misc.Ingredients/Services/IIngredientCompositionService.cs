using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents an ingredient composition service: maintains the composite ingredient DAG
/// (<see cref="IngredientComposition"/> edges) and its transitive closure (<see cref="IngredientClosure"/>)
/// </summary>
public interface IIngredientCompositionService
{
    /// <summary>
    /// Gets the direct child compositions of a (composite) ingredient
    /// </summary>
    /// <param name="parentIngredientId">Parent (composite) ingredient identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the child compositions, ordered by display order
    /// </returns>
    Task<IList<IngredientComposition>> GetChildCompositionsAsync(int parentIngredientId);

    /// <summary>
    /// Gets which of the given ingredients are themselves composite (have at least one direct child
    /// composition), for marking a "multi-ingredient composition" indicator in a grid without an
    /// N+1 lookup per row
    /// </summary>
    /// <param name="ingredientIds">Ingredient identifiers to check</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the subset of <paramref name="ingredientIds"/> that are composite
    /// </returns>
    Task<IList<int>> GetCompositeIngredientIdsAsync(IEnumerable<int> ingredientIds);

    /// <summary>
    /// Gets an ingredient composition by identifier
    /// </summary>
    /// <param name="ingredientCompositionId">Ingredient composition identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ingredient composition
    /// </returns>
    Task<IngredientComposition> GetIngredientCompositionByIdAsync(int ingredientCompositionId);

    /// <summary>
    /// Adds a child (component) ingredient to a composite ingredient. Transactional: validates the candidate
    /// edge against the current closure (self-loop, cycle, and the maximum composition depth), inserts the
    /// edge, and recomputes the closure from scratch, all inside one database transaction.
    /// </summary>
    /// <param name="parentIngredientId">Composite ingredient identifier</param>
    /// <param name="childIngredientId">Component ingredient identifier</param>
    /// <param name="displayOrder">Display order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task AddChildIngredientAsync(int parentIngredientId, int childIngredientId, int displayOrder = 0);

    /// <summary>
    /// Updates the display order of an ingredient composition edge
    /// </summary>
    /// <param name="ingredientCompositionId">Ingredient composition identifier</param>
    /// <param name="displayOrder">Display order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateDisplayOrderAsync(int ingredientCompositionId, int displayOrder);

    /// <summary>
    /// Removes a child ingredient from a composite ingredient, and recomputes the closure
    /// </summary>
    /// <param name="ingredientComposition">Ingredient composition to remove</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RemoveChildIngredientAsync(IngredientComposition ingredientComposition);

    /// <summary>
    /// Recomputes the entire ingredient closure from scratch, from the current set of composition edges.
    /// Exposed so callers that mutate composition edges outside this service (e.g. deleting an ingredient's
    /// own outgoing edges) can keep the closure consistent without duplicating the algorithm.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RecomputeClosureAsync();
}
