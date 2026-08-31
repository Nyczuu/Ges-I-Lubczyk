using Nop.Core;
using Nop.Core.Domain.Localization;

namespace Nop.Plugin.Misc.Ingredients.Domain;

/// <summary>
/// Represents an ingredient (simple or composite) that can be attached to products
/// </summary>
public class Ingredient : BaseEntity, ILocalizedEntity
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the allergen identifier
    /// </summary>
    public int AllergenId { get; set; }

    /// <summary>
    /// Gets or sets the allergen classification
    /// </summary>
    public AllergenType Allergen
    {
        get => (AllergenType)AllergenId;
        set => AllergenId = (int)value;
    }

    /// <summary>
    /// Gets or sets the date and time of instance creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of instance update
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; }
}
