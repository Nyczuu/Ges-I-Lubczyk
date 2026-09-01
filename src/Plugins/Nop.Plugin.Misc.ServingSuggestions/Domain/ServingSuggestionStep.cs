using Nop.Core;
using Nop.Core.Domain.Localization;

namespace Nop.Plugin.Misc.ServingSuggestions.Domain;

/// <summary>
/// Represents one ordered instruction step of a <see cref="ServingSuggestion"/>
/// </summary>
public class ServingSuggestionStep : BaseEntity, ILocalizedEntity
{
    /// <summary>
    /// Gets or sets the serving suggestion identifier
    /// </summary>
    public int ServingSuggestionId { get; set; }

    /// <summary>
    /// Gets or sets the step text
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}
