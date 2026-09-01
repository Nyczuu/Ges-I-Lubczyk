using Nop.Core;
using Nop.Core.Domain.Localization;

namespace Nop.Plugin.Misc.ServingSuggestions.Domain;

/// <summary>
/// Represents a serving suggestion (title, description, image, ordered steps) for a product
/// </summary>
public class ServingSuggestion : BaseEntity, ILocalizedEntity
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the picture identifier - required, no sentinel value
    /// </summary>
    public int PictureId { get; set; }
}
