using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.ServingSuggestions.Services.Events;

/// <summary>
/// Represents an event consumer that removes a product's serving suggestion when the product is deleted.
/// Product is <c>ISoftDeletedEntity</c> (<c>Nop.Core.Domain.Common.ISoftDeletedEntity</c>), so
/// <c>EntityRepository&lt;TEntity&gt;.DeleteAsync</c> only ever issues an UPDATE (never a physical DELETE)
/// for it - a DB-level cascade FK from ServingSuggestion.ProductId to Product would therefore never fire.
/// This consumer is the actual (application-level) cleanup mechanism; EntityDeletedEvent&lt;Product&gt; is
/// still published on every soft-delete, so it is a reliable hook.
/// </summary>
public class ServingSuggestionProductDeletedEventConsumer : IConsumer<EntityDeletedEvent<Product>>
{
    #region Fields

    protected readonly IServingSuggestionService _servingSuggestionService;

    #endregion

    #region Ctor

    public ServingSuggestionProductDeletedEventConsumer(IServingSuggestionService servingSuggestionService)
    {
        _servingSuggestionService = servingSuggestionService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Handle event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(eventMessage.Entity.Id);
        if (servingSuggestion != null)
            await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
    }

    #endregion
}
