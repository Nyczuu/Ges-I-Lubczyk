using Nop.Core;
using Nop.Plugin.Misc.Ingredients.Domain;

namespace Nop.Plugin.Misc.Ingredients.Services;

/// <summary>
/// Represents one language's localizable values for an <see cref="Ingredient"/> write
/// </summary>
/// <param name="LanguageId">Language identifier</param>
/// <param name="Name">Localized name</param>
/// <param name="Description">Localized description</param>
public record IngredientLocalizedValue(int LanguageId, string Name, string Description);

/// <summary>
/// Represents an ingredient service
/// </summary>
public interface IIngredientService
{
    /// <summary>
    /// Gets an ingredient by identifier
    /// </summary>
    /// <param name="ingredientId">Ingredient identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ingredient
    /// </returns>
    Task<Ingredient> GetIngredientByIdAsync(int ingredientId);

    /// <summary>
    /// Gets ingredients by identifiers
    /// </summary>
    /// <param name="ingredientIds">Ingredient identifiers</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ingredients
    /// </returns>
    Task<IList<Ingredient>> GetIngredientsByIdsAsync(int[] ingredientIds);

    /// <summary>
    /// Gets all ingredients
    /// </summary>
    /// <param name="name">Ingredient name to filter by; pass null or empty to load all</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of ingredients
    /// </returns>
    Task<IPagedList<Ingredient>> GetAllIngredientsAsync(string name = null, int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Inserts an ingredient
    /// </summary>
    /// <param name="ingredient">Ingredient</param>
    /// <param name="localizedValues">Localized values to save together with the entity write, in the same transaction</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertIngredientAsync(Ingredient ingredient, IList<IngredientLocalizedValue> localizedValues = null);

    /// <summary>
    /// Updates an ingredient
    /// </summary>
    /// <param name="ingredient">Ingredient</param>
    /// <param name="localizedValues">Localized values to save together with the entity write, in the same transaction</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateIngredientAsync(Ingredient ingredient, IList<IngredientLocalizedValue> localizedValues = null);

    /// <summary>
    /// Deletes an ingredient. Throws if the ingredient is still used as a component of another ingredient,
    /// or is still attached to a product; the message names what still uses it. No cascade.
    /// </summary>
    /// <param name="ingredient">Ingredient</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteIngredientAsync(Ingredient ingredient);
}
