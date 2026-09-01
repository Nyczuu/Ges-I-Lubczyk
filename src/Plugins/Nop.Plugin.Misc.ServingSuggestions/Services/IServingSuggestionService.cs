using Nop.Plugin.Misc.ServingSuggestions.Domain;

namespace Nop.Plugin.Misc.ServingSuggestions.Services;

/// <summary>
/// Represents one language's localizable values for a <see cref="ServingSuggestion"/> write
/// </summary>
/// <param name="LanguageId">Language identifier</param>
/// <param name="Title">Localized title</param>
/// <param name="Description">Localized description</param>
public record ServingSuggestionLocalizedValue(int LanguageId, string Title, string Description);

/// <summary>
/// Represents one language's localizable values for a <see cref="ServingSuggestionStep"/> write
/// </summary>
/// <param name="LanguageId">Language identifier</param>
/// <param name="Text">Localized step text</param>
public record ServingSuggestionStepLocalizedValue(int LanguageId, string Text);

/// <summary>
/// Represents a serving suggestion service
/// </summary>
public interface IServingSuggestionService
{
    /// <summary>
    /// Gets a serving suggestion by identifier
    /// </summary>
    /// <param name="servingSuggestionId">Serving suggestion identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the serving suggestion
    /// </returns>
    Task<ServingSuggestion> GetServingSuggestionByIdAsync(int servingSuggestionId);

    /// <summary>
    /// Gets the serving suggestion of a product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the serving suggestion, or null if the product has none
    /// </returns>
    Task<ServingSuggestion> GetServingSuggestionByProductIdAsync(int productId);

    /// <summary>
    /// Inserts a serving suggestion
    /// </summary>
    /// <param name="servingSuggestion">Serving suggestion</param>
    /// <param name="localizedValues">Localized values to save together with the entity write, in the same transaction</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null);

    /// <summary>
    /// Updates a serving suggestion
    /// </summary>
    /// <param name="servingSuggestion">Serving suggestion</param>
    /// <param name="localizedValues">Localized values to save together with the entity write, in the same transaction</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateServingSuggestionAsync(ServingSuggestion servingSuggestion, IList<ServingSuggestionLocalizedValue> localizedValues = null);

    /// <summary>
    /// Deletes a serving suggestion, its steps, its localized values, and its picture
    /// </summary>
    /// <param name="servingSuggestion">Serving suggestion</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteServingSuggestionAsync(ServingSuggestion servingSuggestion);

    /// <summary>
    /// Gets the steps of a serving suggestion, ordered by display order
    /// </summary>
    /// <param name="servingSuggestionId">Serving suggestion identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the steps
    /// </returns>
    Task<IList<ServingSuggestionStep>> GetServingSuggestionStepsAsync(int servingSuggestionId);

    /// <summary>
    /// Gets a serving suggestion step by identifier
    /// </summary>
    /// <param name="servingSuggestionStepId">Serving suggestion step identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the serving suggestion step
    /// </returns>
    Task<ServingSuggestionStep> GetServingSuggestionStepByIdAsync(int servingSuggestionStepId);

    /// <summary>
    /// Inserts a serving suggestion step
    /// </summary>
    /// <param name="step">Serving suggestion step</param>
    /// <param name="localizedValues">Localized values to save together with the entity write</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null);

    /// <summary>
    /// Updates a serving suggestion step
    /// </summary>
    /// <param name="step">Serving suggestion step</param>
    /// <param name="localizedValues">Localized values to save together with the entity write</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateServingSuggestionStepAsync(ServingSuggestionStep step, IList<ServingSuggestionStepLocalizedValue> localizedValues = null);

    /// <summary>
    /// Deletes a serving suggestion step
    /// </summary>
    /// <param name="step">Serving suggestion step</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteServingSuggestionStepAsync(ServingSuggestionStep step);
}
