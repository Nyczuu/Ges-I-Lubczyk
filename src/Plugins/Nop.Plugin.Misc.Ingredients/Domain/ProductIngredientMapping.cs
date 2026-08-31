using Nop.Core;

namespace Nop.Plugin.Misc.Ingredients.Domain;

/// <summary>
/// Represents the mapping between a product and one of its directly-attached ingredients
/// </summary>
public class ProductIngredientMapping : BaseEntity
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ingredient identifier
    /// </summary>
    public int IngredientId { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}
