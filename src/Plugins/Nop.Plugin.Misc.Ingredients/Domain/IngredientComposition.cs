using Nop.Core;

namespace Nop.Plugin.Misc.Ingredients.Domain;

/// <summary>
/// Represents a direct composition edge: the parent (composite) ingredient is made of the child ingredient
/// </summary>
public class IngredientComposition : BaseEntity
{
    /// <summary>
    /// Gets or sets the composite (parent) ingredient identifier
    /// </summary>
    public int ParentIngredientId { get; set; }

    /// <summary>
    /// Gets or sets the component (child) ingredient identifier
    /// </summary>
    public int ChildIngredientId { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}
